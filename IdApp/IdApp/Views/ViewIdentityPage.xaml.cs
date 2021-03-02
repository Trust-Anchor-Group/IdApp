using System;
using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using System.IO;
using System.Linq;
using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views
{
    /// <summary>
    /// A page to display when the user wants to view an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class ViewIdentityPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ViewIdentityPage"/> class.
        /// </summary>
        public ViewIdentityPage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new ViewIdentityViewModel(
                DependencyService.Resolve<ITagProfile>(),
                DependencyService.Resolve<IUiDispatcher>(),
                DependencyService.Resolve<INeuronService>(),
                this.navigationService,
                DependencyService.Resolve<INetworkService>(),
                DependencyService.Resolve<ILogService>(),
                DependencyService.Resolve<IImageCacheService>());
            InitializeComponent();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns></returns>
        protected override bool OnBackButtonPressed()
        {
            this.HidePhoto();
            this.navigationService.GoBackAsync();
            return true;
        }

        private async void Image_Tapped(object sender, EventArgs e)
        {
            Image image = (Image)sender;
            var streamImageSource = image.BindingContext;

            // TODO: get index of tapped photo.
            var vm = this.GetViewModel<ViewIdentityViewModel>();
            var selectedStreamImageSource = vm.Photos.FirstOrDefault(x => ReferenceEquals(x, streamImageSource));

            MemoryStream stream = await GetViewModel<ViewIdentityViewModel>().GetImageStreamFor(0);
            if(stream != null)
            {
                this.PhotoViewer.IsVisible = true;
                this.PhotoViewer.ShowPhoto(stream);
            }
        }

        private void PhotoViewer_Tapped(object sender, EventArgs e)
        {
            HidePhoto();
        }

        private void HidePhoto()
        {
            this.PhotoViewer.HidePhoto();
            this.PhotoViewer.IsVisible = false;
        }
    }
}
