using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.MyWallet
{
    /// <summary>
    /// A page that allows the user to view the contents of its wallet, pending payments and recent account events.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyWalletPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="MyWalletPage"/> class.
        /// </summary>
		public MyWalletPage()
		{
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.ViewModel = new MyWalletViewModel(
                Types.Instantiate<ITagProfile>(false),
                Types.Instantiate<IUiDispatcher>(false),
                Types.Instantiate<INeuronService>(false),
                this.navigationService ?? Types.Instantiate<INavigationService>(false),
                Types.Instantiate<INetworkService>(false),
                Types.Instantiate<ILogService>(false),
                Types.Instantiate<IContractOrchestratorService>(false),
                Types.Instantiate<IThingRegistryOrchestratorService>(false),
                Types.Instantiate<IEDalerOrchestratorService>(false));

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

        /// <inheritdoc />
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = this.WalletTabBar.Show();
        }

        /// <inheritdoc />
        protected override async void OnDisappearing()
        {
            await this.WalletTabBar.Hide();
            base.OnDisappearing();
        }
    }
}