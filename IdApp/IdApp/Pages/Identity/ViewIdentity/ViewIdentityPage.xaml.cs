using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Identity.ViewIdentity
{
    /// <summary>
    /// A page to display when the user wants to view an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class ViewIdentityPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ViewIdentityPage"/> class.
        /// </summary>
        public ViewIdentityPage()
        {
            this.ViewModel = new ViewIdentityViewModel();

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
			if (this.ViewModel is ViewIdentityViewModel ViewIdentityViewModel)
			{
				Attachment[] attachments = ViewIdentityViewModel.LegalIdentity?.Attachments;
				this.PhotoViewer.ShowPhotos(attachments);
			}
        }
    }
}
