using System;
using Xamarin.Forms;

[assembly: Dependency(typeof(XamarinApp.iOS.CloseApp))]
namespace XamarinApp.iOS
{
    public class CloseApp : ICloseApplication
    {
        /// <summary>
        /// Closes the App.
        /// </summary>
        public void CloseApplication()
        {
            System.Threading.Thread.CurrentThread.Abort();
        }
    }
}