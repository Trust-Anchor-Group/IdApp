using System.ComponentModel;
using IdApp.Services.Navigation;

namespace IdApp.Pages.Contracts.ClientSignature
{
    /// <summary>
    /// A page that displays a client signature.
    /// </summary>
    [DesignTimeVisible(true)]
	public partial class ClientSignaturePage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ClientSignaturePage"/> class.
        /// </summary>
		public ClientSignaturePage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new ClientSignatureViewModel();
			this.InitializeComponent();
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
    }
}
