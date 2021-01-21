using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
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