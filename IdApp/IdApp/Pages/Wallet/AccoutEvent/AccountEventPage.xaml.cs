using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.AccountEvent
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
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.ViewModel = new AccountEventViewModel(
                Types.Instantiate<ITagProfile>(false),
                Types.Instantiate<IUiDispatcher>(false),
                Types.Instantiate<INeuronService>(false),
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