using Android.Content;
using Android.Media;
using Waher.Events;

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
						this.PreferredSampleRate = SampleRate;
					}
				}
				catch (Exception ex)
				{
					Log.Critical(ex, "Error using AudioManager to get AudioManager.PropertyOutputSampleRate");
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
