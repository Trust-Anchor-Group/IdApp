using Android.Media;

namespace IdApp.AR
{
	public partial class AudioItem
	{
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

				ChangeUpdate?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
			}
		}
	}
}
