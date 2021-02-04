using Android.App;
using Android.Provider;
using Tag.Neuron.Xamarin;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.DeviceInformation))]
namespace IdApp.Android
{
    public class DeviceInformation : IDeviceInformation
    {
        /// <summary>
        /// Gets the ID of the device.
        /// </summary>
        public string GetDeviceID()
        {
            return Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
        }
    }
}