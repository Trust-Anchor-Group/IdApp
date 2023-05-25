using Android.Media;

namespace IdApp.AR
{
	public class AudioItem : IAudioItem
	{
		public AudioItem(string path)
		{
			this.FilePath = path;

			if (File.Exists(path))
			{
				Task.Run(() =>
				{
					this.ExtractMetadata();
					MetadataRetrieved?.Invoke(this, EventArgs.Empty);
				});
			}
		}

		void ExtractMetadata()
		{
			MediaMetadataRetriever Retriever = new();
			Retriever.SetDataSource(this.FilePath);

			string DurationString = Retriever.ExtractMetadata(MetadataKey.Duration) ?? "" ;

			if (!string.IsNullOrEmpty(DurationString) && long.TryParse(DurationString, out long durationMS))
			{
				this.Duration = TimeSpan.FromMilliseconds(durationMS);
			}
			else
			{
				this.Duration = null;
			}
		}

		public event EventHandler? MetadataRetrieved;
		public string? FilePath { get; private set; }
		public TimeSpan? Duration { get; private set; }
		public TimeSpan? Position { get; private set; }
	}
}
