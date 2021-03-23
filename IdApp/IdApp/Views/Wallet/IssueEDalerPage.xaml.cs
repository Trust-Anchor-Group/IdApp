using IdApp.ViewModels.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Wallet
{
    /// <summary>
    /// A page that displays a specific claim thing.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class IssueEDalerPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="IssueEDalerPage"/> class.
        /// </summary>
		public IssueEDalerPage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new EDalerUriViewModel(
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