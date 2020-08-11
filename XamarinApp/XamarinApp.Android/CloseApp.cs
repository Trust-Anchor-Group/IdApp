using System;
using Android.OS;

[assembly:Xamarin.Forms.Dependency(typeof(XamarinApp.Droid.CloseApp))]
namespace XamarinApp.Droid
{
    public class CloseApp : ICloseApplication
    {
        /// <summary>
        /// Closes the App.
        /// </summary>
        public void CloseApplication()
        {
            Process.KillProcess(Process.MyPid());
        }
    }
}