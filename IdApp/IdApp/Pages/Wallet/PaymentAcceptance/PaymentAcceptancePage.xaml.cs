using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.PaymentAcceptance
{
    /// <summary>
    /// A page that allows the user to accept an offline payment.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PaymentAcceptancePage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PaymentAcceptancePage"/> class.
        /// </summary>
		public PaymentAcceptancePage()
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
