using Android.Content;
using Android.Media;
using System.Diagnostics;

namespace IdApp.AR
{
	public partial class AudioRecorderService
	{
		partial void Init()
		{
			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.JellyBean)
			{
				try
				{
					//if the below call to AudioManager is blocking and never returning/taking forever, ensure the emulator has proper access to the system mic input
					if ((Android.App.Application.Context.GetSystemService(Context.AudioService) is AudioManager AudioManager) &&
						(AudioManager.GetProperty(AudioManager.PropertyOutputSampleRate) is string Property) &&
						int.TryParse(Property, out int SampleRate))
					{
						Debug.WriteLine($"Setting PreferredSampleRate to {SampleRate} as reported by AudioManager.PropertyOutputSampleRate");
						this.PreferredSampleRate = SampleRate;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error using AudioManager to get AudioManager.PropertyOutputSampleRate: {0}", ex);
					Debug.WriteLine("PreferredSampleRate will remain at the default");
				}
			}
		}

		Task<string> GetDefaultFilePath()
		{
			string UtcNow = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			return Task.FromResult(Path.Combine(Path.GetTempPath(), $"{UtcNow}.wav"));
		}

		void OnRecordingStarting()
		{
		}

		void OnRecordingStopped()
		{
		}
	}
}
