using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page that displays a specific contract.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ViewContractPage"/> class.
        /// </summary>
		public ViewContractPage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new ViewContractViewModel();
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