using Android.App;
using IdApp.DeviceSpecific;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.CloseApplication))]
namespace IdApp.Android
{
    public class CloseApplication : ICloseApplication
    {
        /// <summary>
        /// Closes the application
        /// </summary>
        public Task Close()
        {
            Activity Activity = Application.Context as Activity;    // TODO: returns null. Context points to Application instance.
            Activity?.FinishAffinity();

            Java.Lang.JavaSystem.Exit(0);

            return Task.CompletedTask;
        }
    }
}