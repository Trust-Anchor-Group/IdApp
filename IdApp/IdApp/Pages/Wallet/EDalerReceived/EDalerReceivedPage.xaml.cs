using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.EDalerReceived
{
    /// <summary>
    /// A page that displays information about eDaler received.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EDalerReceivedPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="EDalerReceivedPage"/> class.
        /// </summary>
		public EDalerReceivedPage()
		{
            this.ViewModel = new EDalerReceivedViewModel();

			this.InitializeComponent();
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
