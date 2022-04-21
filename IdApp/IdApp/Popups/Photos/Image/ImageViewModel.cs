using System.Collections.ObjectModel;
using IdApp.Pages;
using IdApp.Services.UI.Photos;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Popups.Photos.Image
{
    /// <summary>
    ///  The class to use as binding context for displaying images.
    /// </summary>
    public class ImageViewModel : BaseViewModel
    {
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="ImageViewModel"/> class.
        /// </summary>
        public ImageViewModel()
        {
            this.Photos = new ObservableCollection<Photo>();
            this.photosLoader = new PhotosLoader(this.Photos);
        }

        /// <summary>
        /// Holds the list of photos to display.
        /// </summary>
        public ObservableCollection<Photo> Photos { get; }

        /// <summary>
        /// See <see cref="IsSwipeEnabled"/>
        /// </summary>
        public static readonly BindableProperty IsSwipeEnabledProperty =
            BindableProperty.Create(nameof(IsSwipeEnabled), typeof(bool), typeof(ImageViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether a user can swipe to see the photos.
        /// </summary>
        public bool IsSwipeEnabled
        {
            get { return (bool) GetValue(IsSwipeEnabledProperty); }
            set { SetValue(IsSwipeEnabledProperty, value); }
        }

        /// <summary>
        /// Loads the attachments photos, if there are any.
        /// </summary>
        /// <param name="attachments">The attachments to load.</param>
        public void LoadPhotos(Attachment[] attachments)
        {
            this.photosLoader.CancelLoadPhotos();
            this.IsSwipeEnabled = false;

            _ = this.photosLoader.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys, () =>
            {
                this.UiSerializer.BeginInvokeOnMainThread(() => this.IsSwipeEnabled = this.Photos.Count > 1);
            });
        }

        /// <summary>
        /// Clears the currently displayed photos.
        /// </summary>
        public void ClearPhotos()
        {
            this.photosLoader.CancelLoadPhotos();
        }
    }
}