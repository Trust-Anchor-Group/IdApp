using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(IdApp.iOS.DeviceInformation))]
namespace IdApp.iOS
{
    public class DeviceInformation : IDeviceInformation
    {
        /// <summary>
        /// Gets the ID of the device.
        /// </summary>
        public string GetDeviceId()
        {
            return UIDevice.CurrentDevice.IdentifierForVendor.ToString();
        }
    }
}