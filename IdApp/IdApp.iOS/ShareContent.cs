using Foundation;
using IdApp.DeviceSpecific;
using System.IO;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(IdApp.iOS.ShareContent))]
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
            UIActivityViewController activityController = new(Items, null);
            UIViewController topController = UIApplication.SharedApplication.KeyWindow.RootViewController;

            while (topController.PresentedViewController is not null)
                topController = topController.PresentedViewController;

            topController.PresentViewController(activityController, true, () => { });
        }
    }
}
