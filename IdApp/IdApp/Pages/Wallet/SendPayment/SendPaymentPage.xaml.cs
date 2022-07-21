using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.SendPayment
{
    /// <summary>
    /// A page that allows the user to realize payments.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SendPaymentPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SendPaymentPage"/> class.
        /// </summary>
		public SendPaymentPage()
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
