using System.ComponentModel;

namespace IdApp.Pages.Contracts.ServerSignature
{
    /// <summary>
    /// A page that displays a server signature.
    /// </summary>
    [DesignTimeVisible(true)]
	public partial class ServerSignaturePage
	{
        /// <summary>
        /// Creates a new instance of the <see cref="ServerSignaturePage"/> class.
        /// </summary>
		public ServerSignaturePage()
		{
            ViewModel = new ServerSignatureViewModel();
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
