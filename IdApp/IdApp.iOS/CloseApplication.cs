using IdApp.DeviceSpecific;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(IdApp.iOS.CloseApplication))]
namespace IdApp.iOS
{
    public class CloseApplication : ICloseApplication
    {
        /// <summary>
        /// Closes the application
        /// </summary>
        public Task Close()
        {
            Thread.CurrentThread.Abort();

            return Task.CompletedTask;
        }
    }
}