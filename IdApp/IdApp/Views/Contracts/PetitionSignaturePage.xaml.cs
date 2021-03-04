using System;
using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page to display when the user is asked to sign data.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class PetitionSignaturePage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="PetitionSignaturePage"/> class.
        /// </summary>
        public PetitionSignaturePage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new PetitionSignatureViewModel();
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            this.PhotoViewer.HidePhotos();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns></returns>
        protected override bool OnBackButtonPressed()
        {
            if (this.PhotoViewer.PhotosAreShowing())
                this.PhotoViewer.HidePhotos();
            else
                this.navigationService.GoBackAsync();
            return true;
        }

        private void Image_Tapped(object sender, EventArgs e)
        {
            Attachment[] attachments = this.GetViewModel<ViewIdentityViewModel>().LegalIdentity?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
