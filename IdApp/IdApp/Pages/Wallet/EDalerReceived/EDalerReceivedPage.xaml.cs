using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.EDalerReceived
{
    /// <summary>
    /// A page that displays information about eDaler received.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EDalerReceivedPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="EDalerReceivedPage"/> class.
        /// </summary>
		public EDalerReceivedPage()
		{
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.ViewModel = new EDalerReceivedViewModel(
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