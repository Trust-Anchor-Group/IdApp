using AVFoundation;
using Foundation;

namespace IdApp.AR
{
	public partial class AudioPlayer
	{
		private AVAudioPlayer? mediaPlayer = null;
		private NSString? currentAVAudioSessionCategory;

		private static AVAudioSessionCategory? requestedAVAudioSessionCategory;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to configure the <see cref="AVAudioSession"/> before playing audio.
		/// </summary>
		public static Action<AVAudioSession>? OnPrepareAudioSession;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to reset or re-configure the <see cref="AVAudioSession"/> after audio playback is complete.
		/// </summary>
		public static Action<AVAudioSession>? OnResetAudioSession;

		public AudioPlayer()
		{
		}

		/// <summary>
		/// Call this method in your iOS project if you'd like the <see cref="AudioPlayer"/> to set the shared <see cref="AVAudioSession"/>
		/// category to the requested <paramref name="category"/> before playing audio and return it to its previous value after playback is complete.
		/// <see cref="OnPrepareAudioSession"/> and <see cref="OnResetAudioSession"/> will also be called before and after each playback operation to allow for further session configuration.
		/// Note that some categories do not support playback.
		/// </summary>
		public static void RequestAVAudioSessionCategory(AVAudioSessionCategory category)
		{
			requestedAVAudioSessionCategory = category;
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

			if (this.mediaPlayer is not null)
			{
				this.mediaPlayer.FinishedPlaying -= this.Player_FinishedPlaying;
			}

			if (requestedAVAudioSessionCategory.HasValue)
			{
				// If the user has called RequestAVAudioSessionCategory(), let's attempt to set that category for them
				//	see: https://developer.apple.com/library/archive/documentation/Audio/Conceptual/AudioSessionProgrammingGuide/AudioSessionCategoriesandModes/AudioSessionCategoriesandModes.html#//apple_ref/doc/uid/TP40007875-CH10
				AVAudioSession AudioSession = AVAudioSession.SharedInstance();

				if (!AudioSession.Category.ToString().EndsWith(requestedAVAudioSessionCategory.Value.ToString()))
				{
					// track the current category, as long as we haven't already done this (or else we may capture the category we're setting below)
					this.currentAVAudioSessionCategory ??= AudioSession.Category;

					NSError? Error = AudioSession.SetCategory(requestedAVAudioSessionCategory.Value);

					if (Error is not null)
					{
						throw new Exception($"Current AVAudioSession category is ({this.currentAVAudioSessionCategory}); Application requested an AVAudioSession category of {requestedAVAudioSessionCategory.Value} but received error when attempting to set it: {Error}");
					}
				}

				// allow for additional audio session config
				OnPrepareAudioSession?.Invoke(AudioSession);
			}

			this.currentAudioItem = AudioItem;
			this.mediaPlayer = AVAudioPlayer.FromUrl(NSUrl.FromFilename(AudioItem.FilePath));

			if (this.mediaPlayer is not null)
			{
				this.mediaPlayer.FinishedPlaying += this.Player_FinishedPlaying;
				this.Play();
			}
		}

		void Player_FinishedPlaying(object sender, AVStatusEventArgs e)
		{
			if (this.currentAVAudioSessionCategory is not null)
			{
				AVAudioSession AudioSession = AVAudioSession.SharedInstance();

				if (AudioSession.SetCategory(this.currentAVAudioSessionCategory, out NSError err))
				{
					this.currentAVAudioSessionCategory = null; //reset this if success, otherwise hang onto it to possibly try again
				}
				else
				{
					// we won't error out here as this likely won't prevent us from stopping properly...
				}

				// allow for additional audio session reset/config
				OnResetAudioSession?.Invoke(AudioSession);
			}
		}

		private double GetPosition()
		{
			return (this.mediaPlayer?.CurrentTime ?? 0) * 1000;
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

				this.mediaPlayer?.Play();
				this.currentAudioItem.IsPlaying = true;
			}
		}
	}
}
