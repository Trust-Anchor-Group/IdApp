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

				string DurationString = Retriever.ExtractMetadata(MetadataKey.Duration) ?? string.Empty;

				if (!string.IsNullOrEmpty(DurationString) && double.TryParse(DurationString, out double Duration))
				{
					this.Duration = Duration;
				}
				else
				{
					this.Duration = 0.5;
				}

				ChangeUpdate?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
			}
		}
	}
}
