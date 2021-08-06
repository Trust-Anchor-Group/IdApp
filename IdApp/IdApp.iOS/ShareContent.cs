using System.IO;
using Foundation;
using UIKit;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.iOS.ShareContent))]
namespace IdApp.iOS
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
            ImageSource Image = ImageSource.FromStream(() => new MemoryStream(PngFile));
            NSObject ImageObject = NSObject.FromObject(Image);
            NSObject MessageObject = NSObject.FromObject(Message);
            NSObject[] Items = new NSObject[] { MessageObject, ImageObject };
            var activityController = new UIActivityViewController(Items, null);
            var topController = UIApplication.SharedApplication.KeyWindow.RootViewController;

            while (topController.PresentedViewController != null)
                topController = topController.PresentedViewController;

            topController.PresentViewController(activityController, true, () => { });
        }
    }
}