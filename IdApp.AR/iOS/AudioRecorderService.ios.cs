using AVFoundation;
using Foundation;
using System.Diagnostics;

namespace IdApp.AR
{
	public partial class AudioRecorderService
	{
		private NSString? currentAVAudioSessionCategory;
		private static AVAudioSessionCategory? requestedAVAudioSessionCategory;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to configure the <see cref="AVAudioSession"/> before recording audio.
		/// </summary>
		public static Action<AVAudioSession>? OnPrepareAudioSession;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to reset or re-configure the <see cref="AVAudioSession"/> after audio recording is complete.
		/// </summary>
		public static Action<AVAudioSession>? OnResetAudioSession;

		partial void Init() { }

		/// <summary>
		/// Call this method in your iOS project if you'd like the <see cref="AudioRecorderService"/> to attempt to set the shared <see cref="AVAudioSession"/>
		/// category to the requested <paramref name="category"/> before recording audio and return it to its previous value after recording is complete.
		/// The default category used will be <see cref="AVAudioSessionCategory.PlayAndRecord"/>.  Note that some categories do not support recording.
		/// </summary>
		public static void RequestAVAudioSessionCategory(AVAudioSessionCategory Category = AVAudioSessionCategory.PlayAndRecord)
		{
			requestedAVAudioSessionCategory = Category;
		}

		Task<string> GetDefaultFilePath()
		{
			string UtcNow = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			return Task.FromResult(Path.Combine(Path.GetTempPath(), $"{UtcNow}.wav"));
		}

		void OnRecordingStarting()
		{
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
		}

		void OnRecordingStopped()
		{
			if (this.currentAVAudioSessionCategory is not null)
			{
				AVAudioSession AudioSession = AVAudioSession.SharedInstance();

				if (AudioSession.SetCategory(this.currentAVAudioSessionCategory, out NSError Error))
				{
					this.currentAVAudioSessionCategory = null; //reset this if success, otherwise hang onto it to possibly try again
				}
				else
				{
					// we won't error out here as this likely won't prevent us from stopping properly... but we will log an issue
					Debug.WriteLine ($"Error attempting to set the AVAudioSession category back to {this.currentAVAudioSessionCategory} :: {Error}");
				}

				// allow for additional audio session reset/config
				OnResetAudioSession?.Invoke(AudioSession);
			}
		}
	}
}
