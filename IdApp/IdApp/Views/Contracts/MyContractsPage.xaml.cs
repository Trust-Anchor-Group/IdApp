using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContractsPage
	{
        private readonly INavigationService navigationService;

        public MyContractsPage()
        : this(true)
        {
        }

        protected MyContractsPage(bool showCreatedContracts)
		{
            this.Title = showCreatedContracts ? AppResources.MyContracts : AppResources.SignedContracts;
            this.navigationService = DependencyService.Resolve<INavigationService>();
			this.ViewModel = new MyContractsViewModel(showCreatedContracts);
			InitializeComponent();
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
	}
}