using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page that displays a list of the current user's contracts.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContractsPage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="MyContractsPage"/> class.
        /// </summary>
        public MyContractsPage()
        : this(true)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MyContractsPage"/> class.
        /// </summary>
        /// <param name="showCreatedContracts">Set to <c>true</c> if created contracts should be shown, <c>false</c> otherwise.</param>
        protected MyContractsPage(bool showCreatedContracts)
		{
            this.Title = showCreatedContracts ? AppResources.MyContracts : AppResources.SignedContracts;
            this.navigationService = DependencyService.Resolve<INavigationService>();
			this.ViewModel = new MyContractsViewModel(showCreatedContracts);
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