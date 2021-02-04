using Android.App;
using Tag.Neuron.Xamarin;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.AppInformation))]
namespace IdApp.Android
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