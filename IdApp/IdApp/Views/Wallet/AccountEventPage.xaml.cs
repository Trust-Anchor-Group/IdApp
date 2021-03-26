using System.Threading.Tasks;
using IdApp.ViewModels.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Wallet
{
    /// <summary>
    /// A page that allows the user to view information about an account event.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AccountEventPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="AccountEventPage"/> class.
        /// </summary>
		public AccountEventPage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new AccountEventViewModel(
                DependencyService.Resolve<ITagProfile>(),
                DependencyService.Resolve<IUiDispatcher>(),
                DependencyService.Resolve<INeuronService>(),
                this.navigationService);

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