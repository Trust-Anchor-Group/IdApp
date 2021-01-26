using Tag.Neuron.Xamarin;
using Xamarin.Forms;
using UIKit;

[assembly: Dependency(typeof(IdApp.iOS.DeviceInformation))]
namespace IdApp.iOS
{
    public class DeviceInformation : IDeviceInformation
    {
        /// <summary>
        /// Gets the ID of the device.
        /// </summary>
        public string GetDeviceID()
        {
            return UIDevice.CurrentDevice.IdentifierForVendor.ToString();
        }
    }
}