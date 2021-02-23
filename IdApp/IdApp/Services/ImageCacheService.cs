using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;

namespace IdApp.Services
{
	///<inheritdoc cref="IImageCacheService"/>
	[Singleton]
	internal sealed partial class ImageCacheService : LoadableService, IImageCacheService
	{
		private static readonly TimeSpan Expiry = TimeSpan.FromHours(24);
		private const string CacheFolderName = "ImageCache";
		private const string KeyPrefix = "ImageCache_";
		private readonly Dictionary<string, CacheEntry> entries;
		private readonly ISettingsService settingsService;
		private readonly ILogService logService;

		/// <summary>
		/// Creates a new instance of the <see cref="ImageCacheService"/> class.
		/// </summary>
		/// <param name="settingsService">The settings service to use.</param>
		/// <param name="logService">The log service to use.</param>
		public ImageCacheService(ISettingsService settingsService, ILogService logService)
		{
			this.entries = new Dictionary<string, CacheEntry>();
			this.settingsService = settingsService;
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
					{
						List<(string, CacheEntry)> cacheEntries = (await this.settingsService.RestoreStateWhereKeyStartsWith<CacheEntry>(KeyPrefix)).ToList();

						foreach ((string key, CacheEntry entry) in cacheEntries)
							this.entries[key] = entry;

						EvictOldEntries();
					}

					this.EndLoad(true);
				}
				catch (Exception e)
				{
					this.logService.LogException(e);
					this.EndLoad(false);
				}
			}
		}

		///<inheritdoc/>
		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				try
				{
					// Wipe any old settings, so we have a clean slate.
					this.settingsService.RemoveStateWhereKeyStartsWith(KeyPrefix);

					foreach (KeyValuePair<string, CacheEntry> entry in entries)
					{
						string key = $"{KeyPrefix}{entry.Key}";
						this.settingsService.SaveState(key, entry.Value);
					}
				}
				catch (Exception e)
				{
					this.logService.LogException(e);
				}

				this.EndLoad(false);
			}

			return Task.CompletedTask;
		}

		///<inheritdoc/>
		public bool TryGet(string url, out MemoryStream stream)
		{
			stream = null;

			EvictOldEntries();

			if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
			{
				if (this.entries.TryGetValue(url, out CacheEntry entry))
				{
					try
					{
						TimeSpan age = DateTime.UtcNow - entry.TimeStamp;
						if (File.Exists(entry.LocalFileName) && age < Expiry)
						{
							stream = new MemoryStream();
							using (FileStream fileStream = File.OpenRead(entry.LocalFileName))
							{
								fileStream.CopyTo(stream);
								stream.Reset();
							}
							return true;
						}
					}
					catch (Exception e)
					{
						this.logService.LogException(e);
					}
				}
			}

			return false;
		}

		///<inheritdoc/>
		public async Task Add(string url, Stream stream)
		{
			if (!string.IsNullOrWhiteSpace(url) &&
				Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) &&
				stream != null &&
				stream.CanRead)
			{
				string cacheFolder = CreateCacheFolderIfNeeded();
				stream.Reset();
				string localFileName = Path.Combine(cacheFolder, $"Image_{Guid.NewGuid()}.jpg");
				using (FileStream outputStream = File.OpenWrite(localFileName))
				{
					await stream.CopyToAsync(outputStream);
				}
				this.entries[url] = new CacheEntry { TimeStamp = DateTime.UtcNow, LocalFileName = localFileName };
			}
		}

		///<inheritdoc/>
		public void Invalidate(string url)
		{
			if (!string.IsNullOrWhiteSpace(url) &&
				Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
			{
				CreateCacheFolderIfNeeded();
				if (this.entries.TryGetValue(url, out CacheEntry entry))
				{
					string fileName = entry.LocalFileName;
					if (File.Exists(fileName))
					{
						try
						{
							File.Delete(fileName);
						}
						catch (Exception e)
						{
							this.logService.LogException(e);
						}
					}

					this.entries.Remove(url);
				}
			}
		}

		private string CreateCacheFolderIfNeeded()
		{
			string cacheFolder = GetCacheFolder();
			if (!Directory.Exists(cacheFolder))
			{
				Directory.CreateDirectory(cacheFolder);
			}

			return cacheFolder;
		}

		private void EvictOldEntries()
		{
			try
			{
				string cacheFolder = CreateCacheFolderIfNeeded();

				// 1. Purge entries that are too old.
				Dictionary<string, CacheEntry> clone = entries.ToDictionary(x => x.Key, x => x.Value);
				foreach (KeyValuePair<string, CacheEntry> entry in clone)
				{
					if ((DateTime.UtcNow - entry.Value.TimeStamp) >= Expiry)
					{
						// Too old, evict from cache.
						this.entries.Remove(entry.Key);
					}
				}

				// 2. Now delete the actual files.
				List<string> fileNames = clone.Select(x => x.Value.LocalFileName).ToList();
				List<string> filesOnDisc = Directory.GetFiles(cacheFolder).ToList();

				var filesToBeDeleted = filesOnDisc.Except(fileNames).ToList();
				foreach (string file in filesToBeDeleted)
				{
					File.Delete(file);
				}
			}
			catch (Exception e)
			{
				this.logService.LogException(e);
			}
		}

		private static string GetCacheFolder()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CacheFolderName);
		}
	}
}