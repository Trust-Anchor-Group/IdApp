using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.PendingPayment
{
    /// <summary>
    /// A page that allows the user to view information about a pending payment.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PendingPaymentPage : IShareQrCode
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PendingPaymentPage"/> class.
        /// </summary>
		public PendingPaymentPage()
		{
            this.ViewModel = new EDalerUriViewModel(this);

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

        /// <summary>
        /// Scrolls to display the QR-code.
        /// </summary>
        public async Task ShowQrCode()
        {
            await this.ScrollView.ScrollToAsync(this.ShareButton, ScrollToPosition.MakeVisible, true);
        }
    }
}
