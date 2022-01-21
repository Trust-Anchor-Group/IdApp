using System;
using System.ComponentModel;

namespace IdApp.Pages.Identity.TransferIdentity
{
    /// <summary>
    /// A page to display when the user wants to view an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class TransferIdentityPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TransferIdentityPage"/> class.
        /// </summary>
        public TransferIdentityPage()
        {
            this.ViewModel = new TransferIdentityViewModel();

            InitializeComponent();
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
    }
}
