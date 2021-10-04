using System.Threading.Tasks;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
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
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new EDalerUriViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiSerializer>(),
                App.Instantiate<INeuronService>(),
                this.navigationService ?? App.Instantiate<INavigationService>(),
                App.Instantiate<INetworkService>(),
                App.Instantiate<ILogService>(),
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