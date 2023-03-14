using FFImageLoading;
using FFImageLoading.Forms;
using IdApp.Services.AttachmentCache;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Html.Elements;
using Waher.Runtime.Temporary;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using IOPath = System.IO.Path;

namespace IdApp.Helpers
{
	/// <summary>
	/// </summary>
	public sealed class AesImageSource : DataUrlImageSource
	{
		/// <summary>
		/// </summary>
		public AesImageSource(Uri Uri) : base(Uri.OriginalString)
		{
			this.Uri = Uri;
		}

		/// <summary>
		/// </summary>
		public AesImageSource() : base(null)
		{
		}

		private IAttachmentCacheService attachmentCacheService;

		/// <summary>
		/// Provides a reference to the attachment cache service.
		/// </summary>
		public IAttachmentCacheService AttachmentCacheService
		{
			get
			{
				if (this.attachmentCacheService is null)
					this.attachmentCacheService = App.Instantiate<IAttachmentCacheService>();

				return this.attachmentCacheService;
			}
		}

		/// <summary>
		/// </summary>
		public static readonly BindableProperty UriProperty = BindableProperty.Create("Uri", typeof(Uri), typeof(AesImageSource), default(Uri),
			propertyChanged: (bindable, oldvalue, newvalue) => ((AesImageSource)bindable).OnUriChanged(), validateValue: (bindable, value) => value == null || ((Uri)value).IsAbsoluteUri);

		static readonly object syncHandle = new object();
		static readonly Dictionary<string, LockingSemaphore> semaphores = new Dictionary<string, LockingSemaphore>();

		/// <summary>
		/// </summary>
		public override bool IsEmpty => this.Uri == null;

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

		async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			Stream stream = await this.GetStreamFromCacheAsync(uri, cancellationToken).ConfigureAwait(false);

			return stream;
		}

		async Task<Stream> GetStreamAsyncUnchecked(string key, Uri uri, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = this.Uri.OriginalString;
			(byte[] Bin, _) = await this.AttachmentCacheService.TryGet(Url).ConfigureAwait(false);

			CancellationToken.ThrowIfCancellationRequested();

			if (Bin is null)
			{
				KeyValuePair<string, TemporaryStream> Content = await InternetContent.GetTempStreamAsync(this.Uri).ConfigureAwait(false);

				CancellationToken.ThrowIfCancellationRequested();

				Content.Value.Position = 0;
				Bin = Content.Value.ToByteArray();

				await this.AttachmentCacheService.Add(Url, string.Empty, false, Bin, Content.Key).ConfigureAwait(false);
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

		void OnUriChanged()
		{
			this.CancellationTokenSource?.Cancel();
			this.OnSourceChanged();
		}
	}

	internal class StreamWrapper : Stream
	{
		readonly Stream wrapped;
		IDisposable additionalDisposable;

		public StreamWrapper(Stream wrapped) : this(wrapped, null)
		{
		}

		public StreamWrapper(Stream wrapped, IDisposable additionalDisposable)
		{
			if (wrapped == null)
				throw new ArgumentNullException("wrapped");

			this.wrapped = wrapped;
			this.additionalDisposable = additionalDisposable;
		}

		public override bool CanRead
		{
			get { return this.wrapped.CanRead; }
		}

		public override bool CanSeek
		{
			get { return this.wrapped.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return this.wrapped.CanWrite; }
		}

		public override long Length
		{
			get { return this.wrapped.Length; }
		}

		public override long Position
		{
			get { return this.wrapped.Position; }
			set { this.wrapped.Position = value; }
		}

		public event EventHandler Disposed;

		public override void Flush()
		{
			this.wrapped.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.wrapped.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return this.wrapped.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			this.wrapped.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.wrapped.Write(buffer, offset, count);
		}

		protected override void Dispose(bool disposing)
		{
			this.wrapped.Dispose();
			Disposed?.Invoke(this, EventArgs.Empty);
			this.additionalDisposable?.Dispose();
			this.additionalDisposable = null;

			base.Dispose(disposing);
		}

#if !NETSTANDARD1_0
		public static async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken, HttpClient client)
		{
			HttpResponseMessage response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
                Xamarin.Forms.Internals.Log.Warning("HTTP Request", $"Could not retrieve {uri}, status code {response.StatusCode}");
				return null;
			}

			// the HttpResponseMessage needs to be disposed of after the calling code is done with the stream
			// otherwise the stream may get disposed before the caller can use it
			return new StreamWrapper(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), response);
		}
#endif
	}

	internal class LockingSemaphore
	{
		static readonly Task completed = Task.FromResult(true);
		readonly Queue<TaskCompletionSource<bool>> waiters = new Queue<TaskCompletionSource<bool>>();
		int currentCount;

		public LockingSemaphore(int initialCount)
		{
			if (initialCount < 0)
				throw new ArgumentOutOfRangeException("initialCount");
			this.currentCount = initialCount;
		}

		public void Release()
		{
			TaskCompletionSource<bool> toRelease = null;
			lock (this.waiters)
			{
				if (this.waiters.Count > 0)
					toRelease = this.waiters.Dequeue();
				else
					++this.currentCount;
			}
			if (toRelease != null)
				toRelease.TrySetResult(true);
		}

		public Task WaitAsync(CancellationToken token)
		{
			lock (this.waiters)
			{
				if (this.currentCount > 0)
				{
					--this.currentCount;
					return completed;
				}
				TaskCompletionSource<bool> waiter = new();
				this.waiters.Enqueue(waiter);
				token.Register(() => waiter.TrySetCanceled());
				return waiter.Task;
			}
		}
	}
}
