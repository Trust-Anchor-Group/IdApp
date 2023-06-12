using AudioToolbox;
using Foundation;
using System.Diagnostics;

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
					this.Duration = AudioFile.EstimatedDuration * 1000;
				}
				else
				{
					this.Duration = 0.5;
				}

				ChangeUpdate?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error getting audio durration : {0}", ex);
			}
		}
	}
}
