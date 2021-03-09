using System;
using System.Linq;
using IdApp.ViewModels;
using Tag.Neuron.Xamarin.Extensions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Views
{
    /// <summary>
    ///  A generic UI component to display an image.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageView
    {
        private const uint DurationInMs = 300;

        /// <summary>
        /// Creates a new instance of the <see cref="ImageView"/> class.
        /// </summary>
        public ImageView()
        {
            InitializeComponent();
            ViewModel = new ImageViewViewModel();
        }

        /// <summary>
        /// Shows the attachments photos in the current view.
        /// </summary>
        /// <param name="attachments">The attachments to show.</param>
        public void ShowPhotos(Attachment[] attachments)
        {
            if (attachments is null || attachments.Length <= 0)
                return;

            var imageAttachments = attachments.GetImageAttachments().ToArray();
            if (imageAttachments.Length <= 0)
                return;

            this.IsVisible = true;
            GetViewModel<ImageViewViewModel>().LoadPhotos(attachments);
            Device.BeginInvokeOnMainThread(async () =>
            {
                await this.PhotoViewer.FadeTo(1d, DurationInMs, Easing.SinIn);
            });
        }

        /// <summary>
        /// Hides the photos from view.
        /// </summary>
        public void HidePhotos()
        {
            this.PhotoViewer.Opacity = 0;
            GetViewModel<ImageViewViewModel>().ClearPhotos();
            this.IsVisible = false;
        }

        private void CloseIcon_Tapped(object sender, EventArgs e)
        {
            HidePhotos();
        }

        /// <summary>
        /// Gets if photos are showing or not.
        /// </summary>
        /// <returns></returns>
        public bool PhotosAreShowing()
        {
            return this.PhotoViewer.Opacity > 0 && this.IsVisible;
        }
    }
}