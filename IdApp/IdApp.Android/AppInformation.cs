using Android.App;
using Tag.Neuron.Xamarin;

[assembly: Xamarin.Forms.Dependency(typeof(XamarinApp.Droid.AppInformation))]
namespace XamarinApp.Droid
{
    public class AppInformation : IAppInformation
    {
        private string version;

        public string GetVersion()
        {
            if (version == null)
            {
                var context = Application.Context;
                var info = context.PackageManager.GetPackageInfo(context.PackageName, 0);
                version = info.VersionName;
            }

            return version;
        }
    }
}