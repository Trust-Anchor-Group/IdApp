using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.NewContract
{
    /// <summary>
    /// A page that allows the user to create a new contract.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="NewContractPage"/> class.
        /// </summary>
		public NewContractPage()
        {
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new NewContractViewModel();
            InitializeComponent();
        }

        ///// <inheritdoc/>
        //protected override void OnAppearing()
        //{
        //    base.OnAppearing();
        //    this.ForceReRender(this.RootScrollView);
        //}

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