using System;
using System.ComponentModel;
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
            this.ViewModel.NavigationService.GoBackAsync();
            return true;
        }

        private void Image_Tapped(object sender, EventArgs e)
        {
            Attachment[] attachments = this.GetViewModel<ViewIdentityViewModel>().LegalIdentity?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
