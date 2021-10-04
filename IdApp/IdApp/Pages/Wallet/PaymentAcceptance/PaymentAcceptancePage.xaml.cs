using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.PaymentAcceptance
{
    /// <summary>
    /// A page that allows the user to accept an offline payment.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PaymentAcceptancePage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="PaymentAcceptancePage"/> class.
        /// </summary>
		public PaymentAcceptancePage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new EDalerUriViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiDispatcher>(),
                App.Instantiate<INeuronService>(),
                this.navigationService ?? App.Instantiate<INavigationService>(),
                App.Instantiate<INetworkService>(),
                App.Instantiate<ILogService>(),
                null);

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