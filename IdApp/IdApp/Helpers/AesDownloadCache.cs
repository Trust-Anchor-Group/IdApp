using FFImageLoading;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Work;
using IdApp.Services.AttachmentCache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP.Concentrator;
using Waher.Runtime.Temporary;
using Xamarin.Forms;

namespace IdApp.Helpers
{
	/// <summary>
	/// </summary>
	public class AesDownloadCache : DownloadCache
	{
		/// <summary>
		/// </summary>
		public AesDownloadCache(Configuration Configuration) : base(Configuration) { }

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

		/// <inheritdoc/>
		public override async Task<CacheStream> DownloadAndCacheIfNeededAsync(string Url, TaskParameter Parameters, Configuration Configuration, CancellationToken Token)
		{
			if (!Url.StartsWith(Constants.UriSchemes.Aes256))
			{
				return await base.DownloadAndCacheIfNeededAsync(Url, Parameters, Configuration, Token).ConfigureAwait(false);
			}

			string FileName = this.MD5Helper.MD5(Url);
			TimeSpan Duration = Parameters.CacheDuration ?? Configuration.DiskCacheDuration;

			Token.ThrowIfCancellationRequested();

			DownloadInformation DownloadInfo = new(Url, Parameters.CustomCacheKey, FileName, false, Duration);
			Parameters.OnDownloadStarted?.Invoke(DownloadInfo);

			(byte[] ResponseBytes, _) = await this.AttachmentCacheService.TryGet(Url).ConfigureAwait(false);

			Token.ThrowIfCancellationRequested();

			if (ResponseBytes is null)
			{
				KeyValuePair<string, TemporaryStream> Content = await InternetContent.GetTempStreamAsync(new Uri(Url)).ConfigureAwait(false);

				Token.ThrowIfCancellationRequested();

				Content.Value.Position = 0;
				ResponseBytes = Content.Value.ToByteArray();

				await this.AttachmentCacheService.Add(Url, string.Empty, false, ResponseBytes, Content.Key).ConfigureAwait(false);
			}

			if (ResponseBytes is null)
			{
				throw new HttpRequestException("No Content");
			}

			Token.ThrowIfCancellationRequested();

			MemoryStream MemoryStream = new(ResponseBytes, false);
			return new CacheStream(MemoryStream, false, null);
		}
	}
}
