using Android.App;
using Android.Provider;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.DeviceInformation))]
namespace IdApp.Android
{
    public class DeviceInformation : IDeviceInformation
    {
        /// <summary>
        /// Gets the ID of the device.
        /// </summary>
        public string GetDeviceId()
        {
            return Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
        }
    }
}