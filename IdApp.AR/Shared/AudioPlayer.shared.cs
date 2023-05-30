namespace IdApp.AR
{
	public partial class AudioPlayer
    {
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
	}
}
