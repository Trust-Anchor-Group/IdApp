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
using Waher.Runtime.Temporary;
using Xamarin.Forms;

namespace IdApp.Helpers
{
	/// <summary>
	/// </summary>
	public class AesImageSource : DataUrlImageSource
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

		/// <summary>
		/// </summary>
		public static readonly BindableProperty UriProperty = BindableProperty.Create("Uri", typeof(Uri), typeof(AesImageSource), default(Uri),
			propertyChanged: (bindable, oldvalue, newvalue) => ((AesImageSource)bindable).OnUriChanged(), validateValue: (bindable, value) => value is null || ((Uri)value).IsAbsoluteUri);

		/// <summary>
		/// </summary>
		public override bool IsEmpty => this.Uri is null;

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
			this.wrapped = wrapped ?? throw new ArgumentNullException("wrapped");
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
		readonly Queue<TaskCompletionSource<bool>> waiters = new();
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
			toRelease?.TrySetResult(true);
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
