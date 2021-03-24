using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Tag.Neuron.Xamarin;

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
#pragma warning disable CS0618 // Type or member is obsolete
            Java.IO.File Path = Environment.GetExternalStoragePublicDirectory("Temp");
#pragma warning restore CS0618 // Type or member is obsolete

            if (!File.Exists(Path.Path))
                Directory.CreateDirectory(Path.Path);

            string FilePath = Path.Path + FileName;
            File.WriteAllBytes(FilePath, PngFile);

            Intent Intent = new Intent(Intent.ActionSend);
            Intent.PutExtra(Intent.ExtraText, Message);
            Intent.SetType("image/png");
            Intent.PutExtra(Intent.ExtraStream, global::Android.Net.Uri.Parse("file://" + FilePath));

            Application.Context.StartActivity(Intent.CreateChooser(Intent, Title));
        }
    }
}