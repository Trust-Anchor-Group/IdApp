using IdApp.ViewModels.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Wallet
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
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new MyWalletViewModel(
                DependencyService.Resolve<ITagProfile>(),
                DependencyService.Resolve<IUiDispatcher>(),
                DependencyService.Resolve<INeuronService>(),
                this.navigationService,
                DependencyService.Resolve<INetworkService>(),
                DependencyService.Resolve<ILogService>());

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
	}
}