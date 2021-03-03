using System.Collections.ObjectModel;
using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    /// <summary>
    ///  The class to use as binding context for displaying images.
    /// </summary>
    public class ImageViewViewModel : BaseViewModel
    {
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="ImageViewViewModel"/> class.
        /// </summary>
        public ImageViewViewModel()
        {
            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(
                DependencyService.Resolve<ILogService>(),
                DependencyService.Resolve<INetworkService>(),
                DependencyService.Resolve<INeuronService>(),
                DependencyService.Resolve<IUiDispatcher>(),
                DependencyService.Resolve<IImageCacheService>(),
                this.Photos);
        }

        /// <summary>
        /// Holds the list of photos to display.
        /// </summary>
        public ObservableCollection<ImageSource> Photos { get; }

        /// <summary>
        /// Loads the attachments photos, if there are any.
        /// </summary>
        /// <param name="attachments">The attachments to load.</param>
        public void LoadPhotos(Attachment[] attachments)
        {
            this.photosLoader.CancelLoadPhotos();
            _ = this.photosLoader.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys);
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