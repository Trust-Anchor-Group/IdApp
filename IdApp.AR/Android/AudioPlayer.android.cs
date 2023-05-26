using Android.Media;
using System;
using System.Diagnostics;

namespace IdApp.AR
{
	public partial class AudioPlayer
	{
		private MediaPlayer? mediaPlayer;
		private AudioItem? currentAudioItem;
		private Timer? updateTimer;

		public AudioPlayer()
		{
		}

		public void Play(AudioItem AudioItem)
		{
			if (this.currentAudioItem != null)
			{
				if (this.currentAudioItem == AudioItem)
				{
					this.Play();
					return;
				}
				else
				{
					this.Pause();
				}
			}

			if (this.mediaPlayer == null)
			{
				this.mediaPlayer = new MediaPlayer();
				this.mediaPlayer.Prepared += this.MediaPlayer_Prepared;
				this.mediaPlayer.Completion += this.MediaPlayer_Completion;
			}

			this.currentAudioItem = AudioItem;

			this.mediaPlayer.Reset();
			//_mediaPlayer.SetVolume(1.0f, 1.0f);

			this.mediaPlayer.SetDataSource(AudioItem.FilePath);
			this.mediaPlayer.PrepareAsync();
		}

		private double GetPosition()
		{
			return this.mediaPlayer?.CurrentPosition ?? 0;
		}

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
				this.currentAudioItem.Position = this.GetPosition();
				this.currentAudioItem.IsPlaying = false;
				this.currentAudioItem = null;
			}
		}

		public void Pause()
		{
			if (this.currentAudioItem is not null)
			{
				this.mediaPlayer?.Pause();
				this.currentAudioItem.Position = this.GetPosition();
				this.currentAudioItem.IsPlaying = false;
			}
		}

		public void Play()
		{
			if (this.currentAudioItem is not null)
			{
				this.updateTimer ??= new Timer(this.UpdateCallback, this, 100, 100);

				this.mediaPlayer?.Start();
				this.currentAudioItem.IsPlaying = true;
			}
		}
	}
}
