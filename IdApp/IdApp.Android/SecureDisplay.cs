using Android.Views;
using IdApp.DeviceSpecific;
using IdApp.Services.UI;
using System;
using System.Threading;
using Waher.Events;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.SecureDisplay))]
namespace IdApp.Android
{
    public class SecureDisplay : ISecureDisplay
	{
		private static Window mainWindow;
		private static bool screenProtected = true;     // App started with screen protected.
		private static Timer protectionTimer = null;

		internal static void SetMainWindow(Window MainWindow, bool ScreenProtected)
		{
			mainWindow = MainWindow;
			screenProtected = ScreenProtected;
		}

		/// <summary>
		/// If screen capture is prohibited or not.
		/// </summary>
		public bool ProhibitScreenCapture
		{
			get => screenProtected;

			set
			{
				try
				{
					protectionTimer?.Dispose();
					protectionTimer = null;

					if (mainWindow is not null && screenProtected != value)
					{
						if (value)
						{
							mainWindow.AddFlags(WindowManagerFlags.Secure);
							protectionTimer = new Timer(this.ProtectionTimerElapsed, null, 1000 * 60 * 60, Timeout.Infinite);
						}
						else
							mainWindow.ClearFlags(WindowManagerFlags.Secure);

						screenProtected = value;
					}
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		private void ProtectionTimerElapsed(object P)
		{
			IUiSerializer UiSerializer = App.Instantiate<IUiSerializer>();
			UiSerializer.BeginInvokeOnMainThread(() => this.ProhibitScreenCapture = false);
		}
	}
}
