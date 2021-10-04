using System.Threading.Tasks;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.RequestPayment
{
    /// <summary>
    /// A page that displays information about eDaler received.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RequestPaymentPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="RequestPaymentPage"/> class.
        /// </summary>
		public RequestPaymentPage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new RequestPaymentViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiDispatcher>(),
                App.Instantiate<INeuronService>(),
                this.navigationService ?? App.Instantiate<INavigationService>(),
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