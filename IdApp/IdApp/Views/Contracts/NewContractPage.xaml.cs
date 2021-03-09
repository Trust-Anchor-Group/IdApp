using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page that allows the user to create a new contract.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage
	{
        private readonly IUiDispatcher uiDispatcher;
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="NewContractPage"/> class.
        /// </summary>
		public NewContractPage()
        {
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new NewContractViewModel();
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.RootScrollView.ForceLayout();
            });
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