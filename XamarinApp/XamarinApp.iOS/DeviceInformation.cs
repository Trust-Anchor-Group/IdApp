using System;
using Xamarin.Forms;
using UIKit;

[assembly: Dependency(typeof(XamarinApp.iOS.DeviceInformation))]
namespace XamarinApp.iOS
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