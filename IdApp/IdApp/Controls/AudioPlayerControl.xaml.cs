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
	/// <summary>
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AudioPlayerControl
	{
		/// <summary>
		/// </summary>
		public AudioPlayerControl()
		{
			this.PauseResumeCommand = new Command(async _ => await this.ExecutePauseResume());
			this.AudioItem.ChangeUpdate += this.AudioItem_ChangeUpdate;

			this.InitializeComponent();
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
		private readonly AudioItem audioItem = new();

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
			get { return this.cancellationTokenSource is not null; }
		}

		/// <summary>
		/// </summary>
		public virtual Task<bool> Cancel()
		{
			if (!this.IsLoading)
			{
				return Task.FromResult(false);
			}

			TaskCompletionSource<bool> tcs = new();
			TaskCompletionSource<bool> original = Interlocked.CompareExchange(ref this.completionSource, tcs, null);

			if (original is null)
			{
				this.cancellationTokenSource.Cancel();
			}
			else
			{
				tcs = original;
			}

			return tcs.Task;
		}

		private void OnLoadingCompleted(bool cancelled)
		{
			if (!this.IsLoading || this.completionSource is null)
			{
				return;
			}

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
			propertyChanged: (bindable, oldvalue, newvalue) => ((AudioPlayerControl)bindable).OnUriChanged(), validateValue: (bindable, value) => value is null || ((Uri)value).IsAbsoluteUri);

		/// <summary>
		/// </summary>
		public bool IsEmpty => this.Uri is null;

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
		public AudioItem AudioItem
		{
			get => this.audioItem;
		}

		/// <summary>
		/// </summary>
		public bool IsLoaded
		{
			get => this.AudioItem.Duration > 0.5;
		}

		private void AudioItem_ChangeUpdate(object Sender, EventArgs e)
		{
			this.OnPropertyChanged(nameof(this.IsLoaded));
		}

		void OnUriChanged()
		{
			this.CancellationTokenSource?.Cancel();
			this.OnSourceChanged();

			this.AudioItem.Initialise(string.Empty);

			Task.Run(async () => {
				try
				{
					string CacheName = GetCacheKey(this.Uri) + ".wav";
					string FullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), CacheName);

					if (File.Exists(FullPath))
					{
						this.AudioItem.Initialise(FullPath);
					}
					else
					{
						using Stream Stream = await this.GetStreamAsync().ConfigureAwait(false);

						if (Stream is not null)
						{
							using FileStream FileStream = File.Create(FullPath);
							await Stream.CopyToAsync(FileStream);
							FileStream.Close();

							this.AudioItem.Initialise(FullPath);
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

		/// <summary>
		/// The command to bind for pausing/resuming the audio
		/// </summary>
		public ICommand PauseResumeCommand { get; }

		private async Task ExecutePauseResume()
		{
			if (this.AudioItem.IsPlaying)
			{
				audioPlayer.Value.Pause();
			}
			else
			{
				audioPlayer.Value.Play(this.AudioItem);
			}

			await Task.CompletedTask;
		}

		/// <summary>
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public async Task<Stream> GetStreamAsync(CancellationToken UserToken = default)
		{
			this.OnLoadingStarted();
			UserToken.Register(this.CancellationTokenSource.Cancel);
			Stream Stream;

			try
			{
				Stream = await this.GetStreamAsync(this.Uri, this.CancellationTokenSource.Token);
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

			return Stream;
		}

		/// <summary>
		/// </summary>
		public override string ToString()
		{
			return $"Uri: {this.Uri}";
		}

		static string GetCacheKey(Uri Uri)
		{
			return Device.PlatformServices.GetHash(Uri.AbsoluteUri);
		}

		async Task<Stream> GetStreamAsync(Uri Uri, CancellationToken CancellationToken = default)
		{
			CancellationToken.ThrowIfCancellationRequested();

			Stream Stream = await this.GetStreamFromCacheAsync(Uri, CancellationToken).ConfigureAwait(false);

			return Stream;
		}

		async Task<Stream> GetStreamAsyncUnchecked(Uri Uri, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = Uri.OriginalString;
			(byte[] Bin, _) = await AttachmentCacheService.TryGet(Url).ConfigureAwait(false);

			CancellationToken.ThrowIfCancellationRequested();

			if (Bin is null)
			{
				KeyValuePair<string, TemporaryStream> Content = await InternetContent.GetTempStreamAsync(Uri).ConfigureAwait(false);

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

		async Task<Stream> GetStreamFromCacheAsync(Uri Uri, CancellationToken CancellationToken)
		{
			string Key = GetCacheKey(Uri);
			LockingSemaphore Sem;

			lock (syncHandle)
			{
				if (semaphores.ContainsKey(Key))
					Sem = semaphores[Key];
				else
					semaphores.Add(Key, Sem = new LockingSemaphore(1));
			}

			try
			{
				await Sem.WaitAsync(CancellationToken);
				Stream Stream = await this.GetStreamAsyncUnchecked(Uri, CancellationToken);

				if (Stream is null || Stream.Length == 0 || !Stream.CanRead)
				{
					Sem.Release();
					return null;
				}

				StreamWrapper Wrapped = new(Stream);
				Wrapped.Disposed += (o, e) => Sem.Release();
				return Wrapped;
			}
			catch (OperationCanceledException)
			{
				Sem.Release();
				throw;
			}
		}
	}
}
