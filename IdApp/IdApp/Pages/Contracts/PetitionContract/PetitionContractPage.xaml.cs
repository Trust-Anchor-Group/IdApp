using System;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contracts.PetitionContract
{
    /// <summary>
    /// A page to display when the user is asked to petition a contract.
    /// </summary>
    [DesignTimeVisible(true)]
	public partial class PetitionContractPage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="PetitionContractPage"/> class.
        /// </summary>
		public PetitionContractPage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new PetitionContractViewModel();
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
            Attachment[] attachments = this.GetViewModel<PetitionContractViewModel>().RequestedContract?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
