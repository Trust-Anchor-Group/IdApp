using AudioToolbox;
using Foundation;

namespace IdApp.AR
{
	public partial class AudioItem
	{
		private void ExtractMetadata()
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

				ChangeUpdate?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
			}
		}
	}
}
