using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

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
			this.InitializeComponent();
        }

        /// <inheritdoc/>
        protected override Task OnDisappearingAsync()
        {
            this.PhotoViewer.HidePhotos();
            return base.OnDisappearingAsync();
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

        private void Image_Tapped(object Sender, EventArgs e)
        {
            Attachment[] attachments = this.GetViewModel<PetitionContractViewModel>().RequestorIdentity?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
