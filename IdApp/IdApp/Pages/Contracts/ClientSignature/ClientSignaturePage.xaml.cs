using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;

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
            this.navigationService = Types.Instantiate<INavigationService>(false);
            ViewModel = new ClientSignatureViewModel();
			InitializeComponent();
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
