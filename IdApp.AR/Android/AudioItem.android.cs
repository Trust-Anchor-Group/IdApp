using Android.Media;
using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.AR
{
	public class AudioItem : ObservableObject, IAudioItem
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

				MetadataRetrieved?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
			}
		}

		public event EventHandler? MetadataRetrieved;
		public string FilePath { get; private set; }
		public TimeSpan? Duration { get; private set; }
		public TimeSpan Position { get; private set; }
	}
}
