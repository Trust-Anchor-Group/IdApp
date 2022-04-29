using Android.App;
using Android.Content;
using IdApp.DeviceSpecific;
using System.IO;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.ShareContent))]
namespace IdApp.Android
{
    public class ShareContent : IShareContent
    {
        /// <summary>
        /// Shares an image in PNG format.
        /// </summary>
        /// <param name="PngFile">Binary representation (PNG format) of image.</param>
        /// <param name="Message">Message to send with image.</param>
        /// <param name="Title">Title for operation.</param>
        /// <param name="FileName">Filename of image file.</param>
        public void ShareImage(byte[] PngFile, string Message, string Title, string FileName)
        {
            Java.IO.File dir = Application.Context.GetExternalFilesDir("");

            if (!Directory.Exists(dir.Path))
                Directory.CreateDirectory(dir.Path);

            Java.IO.File fileDir = new(dir.AbsolutePath + (Java.IO.File.Separator + FileName));

            File.WriteAllBytes(fileDir.Path, PngFile);

            Intent Intent = new(Intent.ActionSend);
            Intent.PutExtra(Intent.ExtraText, Message);
            Intent.SetType("image/png");

            Intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            Intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
            Intent.PutExtra(Intent.ExtraStream, FileProvider.GetUriForFile(Application.Context, "com.tag.IdApp.fileprovider", fileDir));

            Intent? myIntent = Intent.CreateChooser(Intent, Title);
            myIntent?.AddFlags(ActivityFlags.NewTask);

            Application.Context.StartActivity(myIntent);
        }
    }
}