using System;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contracts.PetitionSignature
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
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new PetitionSignatureViewModel();
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnDisappearing()
        {
            this.PhotoViewer.HidePhotos();
            base.OnDisappearing();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns>Whether or not the back navigation was handled</returns>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }

        private void Image_Tapped(object sender, EventArgs e)
        {
            Attachment[] attachments = this.GetViewModel<PetitionSignatureViewModel>().RequestorIdentity?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
