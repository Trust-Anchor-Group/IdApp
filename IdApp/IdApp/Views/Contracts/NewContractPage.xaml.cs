using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage
	{
        private readonly INavigationService navigationService;

		public NewContractPage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new NewContractViewModel();
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
	}
}