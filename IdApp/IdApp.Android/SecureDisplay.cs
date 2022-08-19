using Android.App;
using Android.Views;
using IdApp.DeviceSpecific;
using System.Threading;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.SecureDisplay))]
namespace IdApp.Android
{
    public class SecureDisplay : ISecureDisplay
	{
		private static bool screenProtected = true;     // App started with screen protected.
		private static Timer protectionTimer = null;

		/// <summary>
		/// If screen capture is prohibited or not.
		/// </summary>
		public bool ProhibitScreenCapture
		{
			get => screenProtected;

			set
			{
				Activity Activity = Application.Context as Activity;    // TODO: returns null. Context points to Application instance.

				protectionTimer?.Dispose();
				protectionTimer = null;

				if (value)
				{
					Activity.Window.AddFlags(WindowManagerFlags.Secure);

					protectionTimer = new Timer(this.ProtectionTimerElapsed, null, 1000 * 60 * 60, Timeout.Infinite);
				}
				else
					Activity.Window.ClearFlags(WindowManagerFlags.Secure);

				screenProtected = value;
			}
		}

		private void ProtectionTimerElapsed(object P)
		{
			this.ProhibitScreenCapture = false;
		}

	}
}
