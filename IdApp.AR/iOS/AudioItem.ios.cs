using AudioToolbox;
using Foundation;
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
				AudioFile? AudioFile = AudioFile.Open(new NSUrl(this.FilePath), AudioFilePermission.Read);

				if (AudioFile is not null)
				{
					this.Duration = TimeSpan.FromSeconds(AudioFile.EstimatedDuration);
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
