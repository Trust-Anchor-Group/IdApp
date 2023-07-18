using Android.Media;

namespace IdApp.AR
{
	public partial class AudioPlayer
	{
		private MediaPlayer? mediaPlayer;

		public AudioPlayer()
		{
		}

		public void Play(AudioItem AudioItem)
		{
			if (this.currentAudioItem is not null)
			{
				if (this.currentAudioItem == AudioItem)
				{
					// was paused, resume
					this.Play();
					return;
				}
				else
				{
					this.Stop();
				}
			}

			if (this.mediaPlayer is null)
			{
				this.mediaPlayer = new MediaPlayer();
				this.mediaPlayer.Prepared += this.MediaPlayer_Prepared;
				this.mediaPlayer.Completion += this.MediaPlayer_Completion;
			}

			this.currentAudioItem = AudioItem;

			this.mediaPlayer.Reset();
			//this.mediaPlayer.Looping = true;
			//_mediaPlayer.SetVolume(1.0f, 1.0f);

			this.mediaPlayer.SetDataSource(AudioItem.FilePath);
			this.mediaPlayer.PrepareAsync();
		}

		private double GetPosition()
		{
			return this.mediaPlayer?.CurrentPosition ?? 0;
		}

		private void MediaPlayer_Prepared(object sender, EventArgs e)
		{
			this.Play();
		}

		private void MediaPlayer_Completion(object sender, EventArgs e)
		{
			this.Stop();
		}

		public void Stop()
		{
			if (this.updateTimer is not null)
			{
				this.updateTimer.Dispose();
				this.updateTimer = null;
			}

			if (this.currentAudioItem is not null)
			{
				this.mediaPlayer?.Stop();
				this.currentAudioItem.SetPosition(this.GetPosition());
				this.currentAudioItem.SetIsPlaying(false);
				this.currentAudioItem = null;
			}
		}

		public void Pause()
		{
			if (this.currentAudioItem is not null)
			{
				this.mediaPlayer?.Pause();
				this.currentAudioItem.SetPosition(this.GetPosition());
				this.currentAudioItem.SetIsPlaying(false);
			}
		}

		public void Play()
		{
			if (this.currentAudioItem is not null)
			{
				this.updateTimer ??= new Timer(this.UpdateCallback, this, 100, 100);

				this.mediaPlayer?.Start();
				this.currentAudioItem.SetIsPlaying(true);
			}
		}
	}
}
