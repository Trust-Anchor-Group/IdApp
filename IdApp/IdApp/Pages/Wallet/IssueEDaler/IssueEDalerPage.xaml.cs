using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.IssueEDaler
{
    /// <summary>
    /// A page that allows the user to receive newly issued eDaler.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class IssueEDalerPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="IssueEDalerPage"/> class.
        /// </summary>
		public IssueEDalerPage()
		{
            this.ViewModel = new EDalerUriViewModel(null);

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
