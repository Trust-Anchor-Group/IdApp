using AVFoundation;
using Foundation;

namespace IdApp.AR
{
	public class AudioItem : IAudioItem
	{
		public AudioItem(string path)
		{
			this.FilePath = path;
			Task.Run(this.ExtractMetadata);
		}

		void ExtractMetadata()
		{
			try
			{
				AVAsset Asset = AVAsset.FromUrl(new NSUrl(this.FilePath ?? string.Empty));
				this.Duration = TimeSpan.FromSeconds(Asset.Duration.Seconds);

				AVAssetReader AssetReader = new(Asset, out NSError Error);
				var Duration = AssetReader.Asset.Duration.Value;
				var Timescale = AssetReader.Asset.Duration.TimeScale;
				var TotalDuration = Duration / Timescale;

				/*
				await asset.LoadValuesTaskAsync(assetsToLoad.ToArray()).ConfigureAwait(false);

				Dictionary<string?, AVMetadataItem> metadataDict = asset.CommonMetadata.ToDictionary(t => t.CommonKey, t => t);

				if (string.IsNullOrEmpty(mediaItem.Album))
					mediaItem.Album = metadataDict.GetValueOrDefault(AVMetadata.CommonKeyAlbumName)?.Value.ToString();
				*/

				MetadataRetrieved?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
			}
		}

		public event EventHandler? MetadataRetrieved;
		public string? FilePath { get; private set; }
		public TimeSpan? Duration { get; private set; }
		public TimeSpan? Position { get; private set; }
	}
}
