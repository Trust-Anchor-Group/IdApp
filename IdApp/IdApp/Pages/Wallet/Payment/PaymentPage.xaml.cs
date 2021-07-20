using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.Payment
{
    /// <summary>
    /// A page that allows the user to realize payments.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PaymentPage : IShareQrCode
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="PaymentPage"/> class.
        /// </summary>
		public PaymentPage()
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
        /// <returns></returns>
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