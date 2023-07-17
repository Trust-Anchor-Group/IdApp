namespace IdApp.AR
{
	public partial class AudioPlayer
    {
#pragma warning disable CS0649, CS0169, IDE0051, IDE0044
		private AudioItem? currentAudioItem;
		private Timer? updateTimer;

		private void UpdateCallback(object P)
		{
			if (P is AudioPlayer AudioPlayer)
			{
				AudioItem? AudioItem = AudioPlayer.currentAudioItem;

				if (AudioItem is not null)
				{
					AudioItem.Position = AudioPlayer.GetPosition();
				}
			}
		}
#pragma warning restore CS0649, CS0169, IDE0051, IDE0044
	}
}
