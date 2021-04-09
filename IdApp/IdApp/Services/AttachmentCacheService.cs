using System;
using System.IO;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace IdApp.Services
{
	///<inheritdoc cref="IAttachmentCacheService"/>
	[Singleton]
	internal sealed partial class AttachmentCacheService : LoadableService, IAttachmentCacheService
	{
		private static readonly TimeSpan ExpiryTemporary = TimeSpan.FromHours(24);
		private const string CacheFolderName = "Attachments";
		private readonly ILogService logService;

		/// <summary>
		/// Creates a new instance of the <see cref="AttachmentCacheService"/> class.
		/// </summary>
		/// <param name="logService">The log service to use.</param>
		public AttachmentCacheService(ILogService logService)
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
				if (string.IsNullOrWhiteSpace(Url) || !Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
					return (null, string.Empty);

				CacheEntry Entry = await Database.FindFirstDeleteRest<CacheEntry>(new FilterFieldEqualTo("Url", Url));
				if (Entry is null)
					return (null, string.Empty);

				bool Exists = File.Exists(Entry.LocalFileName);

				if (DateTime.UtcNow >= Entry.Expires || !Exists)
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
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
		/// <param name="Permanent">If attachment is permanent or temporary.</param>
		/// <param name="Data">Binary data of image</param>
		/// <param name="ContentType">Content-Type of data.</param>
		public async Task Add(string Url, string ParentId, bool Permanent, byte[] Data, string ContentType)
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
			DateTime Expires = Permanent ? DateTime.MaxValue : (DateTime.UtcNow + ExpiryTemporary);

			if (Entry is null)
			{
				Entry = new CacheEntry()
				{
					Expires = Expires,
					ParentId = ParentId,
					LocalFileName = Path.Combine(CacheFolder, Guid.NewGuid().ToString() + ".bin"),
					Url = Url,
					ContentType = ContentType
				};

				await Database.Insert(Entry);
			}
			else
			{
				Entry.Expires = Expires;
				Entry.ParentId = ParentId;
				Entry.ContentType = ContentType;

				await Database.Update(Entry);
			}

			File.WriteAllBytes(Entry.LocalFileName, Data);
		}

		/// <summary>
		/// Makes items in the cache, belonging to a given parent object, temporary.
		/// </summary>
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
		public async Task MakeTemporary(string ParentId)
		{
			foreach (CacheEntry Entry in await Database.Find<CacheEntry>(new FilterFieldEqualTo("ParentId", ParentId)))
			{
				if (Entry.Expires == DateTime.MaxValue)
				{
					Entry.Expires = DateTime.UtcNow + ExpiryTemporary;
					await Database.Update(Entry);
				}
			}
		}

		/// <summary>
		/// Makes items in the cache, belonging to a given parent object, permanent.
		/// </summary>
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
		public async Task MakePermanent(string ParentId)
		{
			foreach (CacheEntry Entry in await Database.Find<CacheEntry>(new FilterFieldEqualTo("ParentId", ParentId)))
			{
				if (Entry.Expires != DateTime.MaxValue)
				{
					Entry.Expires = DateTime.MaxValue;
					await Database.Update(Entry);
				}
			}
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
				foreach (CacheEntry Entry in await Database.FindDelete<CacheEntry>(new FilterFieldLesserOrEqualTo("Expires", DateTime.UtcNow)))
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