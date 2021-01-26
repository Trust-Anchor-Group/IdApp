using Foundation;
using Tag.Neuron.Xamarin;
using Xamarin.Forms;

[assembly: Dependency(typeof(XamarinApp.iOS.AppInformation))]
namespace XamarinApp.iOS
{
    public class AppInformation : IAppInformation
    {
        private string version;

        public string GetVersion()
        {
            if (version == null)
            {
                version = NSBundle.MainBundle.InfoDictionary[new NSString("CFBundleVersion")].ToString();
            }

            return version;
        }
    }
}