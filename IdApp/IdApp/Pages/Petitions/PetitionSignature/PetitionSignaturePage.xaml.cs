using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Petitions.PetitionSignature
{
	/// <summary>
	/// A page to display when the user is asked to sign data.
	/// </summary>
	[DesignTimeVisible(true)]
    public partial class PetitionSignaturePage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PetitionSignaturePage"/> class.
        /// </summary>
        public PetitionSignaturePage()
        {
            this.ViewModel = new PetitionSignatureViewModel();
			this.InitializeComponent();
        }

        /// <inheritdoc/>
        protected override Task OnDisappearingAsync()
        {
            this.PhotoViewer.HidePhotos();
            return base.OnDisappearingAsync();
        }

        private void Image_Tapped(object Sender, EventArgs e)
        {
			if (this.ViewModel is PetitionSignatureViewModel PetitionSignatureViewModel)
			{
				Attachment[] attachments = PetitionSignatureViewModel.RequestorIdentity?.Attachments;
				this.PhotoViewer.ShowPhotos(attachments);
			}
        }
    }
}
