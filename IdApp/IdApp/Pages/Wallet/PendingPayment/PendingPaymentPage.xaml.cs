using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
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
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="PendingPaymentPage"/> class.
        /// </summary>
		public PendingPaymentPage()
		{
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.ViewModel = new EDalerUriViewModel(
                Types.Instantiate<ITagProfile>(false),
                Types.Instantiate<IUiDispatcher>(false),
                Types.Instantiate<INeuronService>(false),
                this.navigationService ?? Types.Instantiate<INavigationService>(false),
                Types.Instantiate<INetworkService>(false),
                Types.Instantiate<ILogService>(false),
                this);

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

        /// <summary>
        /// Scrolls to display the QR-code.
        /// </summary>
        public async Task ShowQrCode()
        {
            await this.ScrollView.ScrollToAsync(this.ShareButton, ScrollToPosition.MakeVisible, true);
        }
    }
}