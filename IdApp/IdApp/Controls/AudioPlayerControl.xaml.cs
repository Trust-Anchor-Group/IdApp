using IdApp.Helpers;
using IdApp.Services.AttachmentCache;
using System.Collections.Generic;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using Waher.Content;
using Waher.Runtime.Temporary;
using FFImageLoading;
using System.Windows.Input;
using IdApp.AR;

namespace IdApp.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]

	/// <summary>
	/// </summary>
	public partial class AudioPlayerControl
	{
		/// <summary>
		/// </summary>
		public AudioPlayerControl()
		{
			this.InitializeComponent();

			this.PauseResumeCommand = new Command(async _ => await this.ExecutePauseResume(), _ => this.CanExecutePauseResume());
		}

		private static readonly Lazy<AudioPlayer> audioPlayer = new(() => {
			return new AudioPlayer();
		}, LazyThreadSafetyMode.PublicationOnly);

		private static readonly object syncHandle = new();
		private static readonly Dictionary<string, LockingSemaphore> semaphores = new();
		private static IAttachmentCacheService attachmentCacheService;

		/// <summary>
		/// Provides a reference to the attachment cache service.
		/// </summary>
		protected static IAttachmentCacheService AttachmentCacheService
		{
			get
			{
				attachmentCacheService ??= App.Instantiate<IAttachmentCacheService>();
				return attachmentCacheService;
			}
		}

		private CancellationTokenSource cancellationTokenSource;
		private TaskCompletionSource<bool> completionSource;
		private readonly WeakEventManager weakEventManager = new();
		private string filePath = null;

		/// <summary>
		/// </summary>
		protected CancellationTokenSource CancellationTokenSource
		{
			get { return this.cancellationTokenSource; }
			private set
			{
				if (this.cancellationTokenSource == value)
					return;
				this.cancellationTokenSource?.Cancel();
				this.cancellationTokenSource = value;
			}
		}

		bool IsLoading
		{
			get { return this.cancellationTokenSource != null; }
		}

		/// <summary>
		/// </summary>
		public virtual Task<bool> Cancel()
		{
			if (!this.IsLoading)
				return Task.FromResult(false);

			TaskCompletionSource<bool> tcs = new();
			TaskCompletionSource<bool> original = Interlocked.CompareExchange(ref this.completionSource, tcs, null);
			if (original == null)
			{
				this.cancellationTokenSource.Cancel();
			}
			else
				tcs = original;

			return tcs.Task;
		}

		private void OnLoadingCompleted(bool cancelled)
		{
			if (!this.IsLoading || this.completionSource == null)
				return;

			TaskCompletionSource<bool> tcs = Interlocked.Exchange(ref this.completionSource, null);
			tcs?.SetResult(cancelled);

			lock (syncHandle)
			{
				this.CancellationTokenSource = null;
			}
		}

		private void OnLoadingStarted()
		{
			lock (syncHandle)
			{
				this.CancellationTokenSource = new CancellationTokenSource();
			}
		}

		/// <summary>
		/// </summary>
		private void OnSourceChanged()
		{
			this.weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(SourceChanged));
		}

		internal event EventHandler SourceChanged
		{
			add { this.weakEventManager.AddEventHandler(value); }
			remove { this.weakEventManager.RemoveEventHandler(value); }
		}

		/// <summary>
		/// </summary>
		public static readonly BindableProperty UriProperty = BindableProperty.Create(nameof(Uri), typeof(Uri), typeof(AudioPlayerControl), default(Uri),
			propertyChanged: (bindable, oldvalue, newvalue) => ((AudioPlayerControl)bindable).OnUriChanged(), validateValue: (bindable, value) => value == null || ((Uri)value).IsAbsoluteUri);

		/// <summary>
		/// </summary>
		public bool IsEmpty => this.Uri == null;

		/// <summary>
		/// </summary>
		[Xamarin.Forms.TypeConverter(typeof(Xamarin.Forms.UriTypeConverter))]
		public Uri Uri
		{
			get { return (Uri)this.GetValue(UriProperty); }
			set { this.SetValue(UriProperty, value); }
		}

		/// <summary>
		/// </summary>
		public string FilePath
		{
			get => this.filePath;
			private set
			{
				this.filePath = value;
				this.OnPropertyChanged(nameof(this.IsLoaded));
			}
		}

		/// <summary>
		/// </summary>
		public bool IsLoaded
		{
			get => !string.IsNullOrEmpty(this.FilePath);
		}

		void OnUriChanged()
		{
			this.CancellationTokenSource?.Cancel();
			this.OnSourceChanged();
			this.FilePath = null;

			Task.Run(async () => {
				try
				{
					string CacheName = GetCacheKey(this.Uri) + ".wav";
					string FullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), CacheName);

					if (File.Exists(FullPath))
					{
						this.FilePath = FullPath;
					}
					else
					{
						using Stream Stream = await this.GetStreamAsync().ConfigureAwait(false);

						if (Stream is not null)
						{
							using FileStream FileStream = File.Create(FullPath);
							await Stream.CopyToAsync(FileStream);
							FileStream.Close();

							this.FilePath = FullPath;
						}
					}
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					Xamarin.Forms.Internals.Log.Warning("Image Loading", $"Error getting stream for {this.Uri}: {ex}");
				}
			});
		}

		public bool IsPlaying
		{
			get { return false; }
		}

		/// <summary>
		/// The command to bind for pausing/resuming the audio
		/// </summary>
		public ICommand PauseResumeCommand { get; }

		private bool CanExecutePauseResume()
		{
			return true; //  this.IsRecordingAudio && audioRecorder.Value.IsRecording;
		}

		private async Task ExecutePauseResume()
		{
			await Task.CompletedTask;
		}

		/// <summary>
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public async Task<Stream> GetStreamAsync(CancellationToken userToken = default)
		{
			this.OnLoadingStarted();
			userToken.Register(this.CancellationTokenSource.Cancel);
			Stream stream;

			try
			{
				stream = await this.GetStreamAsync(this.Uri, this.CancellationTokenSource.Token);
				this.OnLoadingCompleted(false);
			}
			catch (OperationCanceledException)
			{
				this.OnLoadingCompleted(true);
				throw;
			}
			catch (Exception ex)
			{
				Xamarin.Forms.Internals.Log.Warning("Image Loading", $"Error getting stream for {this.Uri}: {ex}");
				throw;
			}

			return stream;
		}

		/// <summary>
		/// </summary>
		public override string ToString()
		{
			return $"Uri: {this.Uri}";
		}

		static string GetCacheKey(Uri uri)
		{
			return Device.PlatformServices.GetHash(uri.AbsoluteUri);
		}

		async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			Stream stream = await this.GetStreamFromCacheAsync(uri, cancellationToken).ConfigureAwait(false);

			return stream;
		}

		async Task<Stream> GetStreamAsyncUnchecked(string key, Uri uri, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = this.Uri.OriginalString;
			(byte[] Bin, _) = await AttachmentCacheService.TryGet(Url).ConfigureAwait(false);

			CancellationToken.ThrowIfCancellationRequested();

			if (Bin is null)
			{
				KeyValuePair<string, TemporaryStream> Content = await InternetContent.GetTempStreamAsync(this.Uri).ConfigureAwait(false);

				CancellationToken.ThrowIfCancellationRequested();

				Content.Value.Position = 0;
				Bin = Content.Value.ToByteArray();

				await AttachmentCacheService.Add(Url, string.Empty, false, Bin, Content.Key).ConfigureAwait(false);
			}

			if (Bin is not null)
			{
				return new MemoryStream(Bin);
			}

			return null;
		}

		async Task<Stream> GetStreamFromCacheAsync(Uri uri, CancellationToken cancellationToken)
		{
			string key = GetCacheKey(uri);
			LockingSemaphore sem;
			lock (syncHandle)
			{
				if (semaphores.ContainsKey(key))
					sem = semaphores[key];
				else
					semaphores.Add(key, sem = new LockingSemaphore(1));
			}

			try
			{
				await sem.WaitAsync(cancellationToken);
				Stream stream = await this.GetStreamAsyncUnchecked(key, uri, cancellationToken);
				if (stream == null || stream.Length == 0 || !stream.CanRead)
				{
					sem.Release();
					return null;
				}
				StreamWrapper wrapped = new(stream);
				wrapped.Disposed += (o, e) => sem.Release();
				return wrapped;
			}
			catch (OperationCanceledException)
			{
				sem.Release();
				throw;
			}
		}
	}
}
