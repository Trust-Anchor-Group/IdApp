using System;
using System.IO;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Tag.Neuron.Xamarin.Services;

namespace IdApp.Services
{
	///<inheritdoc cref="IImageCacheService"/>
	[Singleton]
	internal sealed partial class ImageCacheService : LoadableService, IImageCacheService
	{
		private static readonly TimeSpan Expiry = TimeSpan.FromHours(24);
		private const string CacheFolderName = "ImageCache";
		private readonly ILogService logService;

		/// <summary>
		/// Creates a new instance of the <see cref="ImageCacheService"/> class.
		/// </summary>
		/// <param name="logService">The log service to use.</param>
		public ImageCacheService(ILogService logService)
		{
			this.logService = logService;
		}

		///<inheritdoc/>
		public override async Task Load(bool isResuming)
		{
			if (this.BeginLoad())
			{
				try
				{
					CreateCacheFolderIfNeeded();

					if (!isResuming)
						await EvictOldEntries();

					this.EndLoad(true);
				}
				catch (Exception e)
				{
					this.logService.LogException(e);
					this.EndLoad(false);
				}
			}
		}

		/// <summary>
		/// Tries to get a cached image given the specified url.
		/// </summary>
		/// <param name="Url">The url of the image to get.</param>
		/// <returns>If entry was found in the cache, the binary data of the image together with the Content-Type of the data.</returns>
		public async Task<(byte[], string)> TryGet(string Url)
		{
			try
			{
				await EvictOldEntries();

				if (string.IsNullOrWhiteSpace(Url) || !Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
					return (null, string.Empty);

				CacheEntry Entry = await Database.FindFirstDeleteRest<CacheEntry>(new FilterFieldEqualTo("Url", Url));
				if (Entry is null)
					return (null, string.Empty);

				TimeSpan Age = DateTime.UtcNow - Entry.TimeStamp;
				bool Exists = File.Exists(Entry.LocalFileName);

				if (Age >= Expiry || !Exists)
				{
					if (Exists)
						File.Delete(Entry.LocalFileName);

					await Database.Delete(Entry);

					return (null, string.Empty);
				}

				return (File.ReadAllBytes(Entry.LocalFileName), Entry.ContentType);
			}
			catch (Exception e)
			{
				this.logService.LogException(e);
				return (null, string.Empty);
			}
		}

		/// <summary>
		/// Adds an image to the cache.
		/// </summary>
		/// <param name="Url">The url, which is the key for accessing it later.</param>
		/// <param name="Data">Binary data of image</param>
		/// <param name="ContentType">Content-Type of data.</param>
		public async Task Add(string Url, byte[] Data, string ContentType)
		{
			if (string.IsNullOrWhiteSpace(Url) ||
				!Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute) ||
				Data is null ||
				string.IsNullOrWhiteSpace(ContentType))
			{
				return;
			}

			string CacheFolder = CreateCacheFolderIfNeeded();

			CacheEntry Entry = await Database.FindFirstDeleteRest<CacheEntry>(new FilterFieldEqualTo("Url", Url));

			if (Entry is null)
			{
				Entry = new CacheEntry()
				{
					TimeStamp = DateTime.UtcNow,
					LocalFileName = Path.Combine(CacheFolder, Guid.NewGuid().ToString() + ".bin"),
					Url = Url,
					ContentType = ContentType
				};

				await Database.Insert(Entry);
			}
			else
			{
				Entry.TimeStamp = DateTime.UtcNow;
				Entry.ContentType = ContentType;

				await Database.Update(Entry);
			}

			File.WriteAllBytes(Entry.LocalFileName, Data);
		}

		private string CreateCacheFolderIfNeeded()
		{
			string CacheFolder = GetCacheFolder();

			if (!Directory.Exists(CacheFolder))
				Directory.CreateDirectory(CacheFolder);

			return CacheFolder;
		}

		private static string GetCacheFolder()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CacheFolderName);
		}

		private async Task EvictOldEntries()
		{
			try
			{
				foreach (CacheEntry Entry in await Database.FindDelete<CacheEntry>(
					new FilterFieldLesserOrEqualTo("TimeStamp", DateTime.UtcNow.Subtract(Expiry))))
				{
					try
					{
						if (File.Exists(Entry.LocalFileName))
							File.Delete(Entry.LocalFileName);
					}
					catch (Exception ex)
					{
						this.logService.LogException(ex);
					}
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
			}
		}
	}
}