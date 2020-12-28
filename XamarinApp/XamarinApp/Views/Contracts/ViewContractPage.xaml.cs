using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
        private readonly INavigationService navigationService;

		public ViewContractPage(Contract contract, bool readOnly)
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new ViewContractViewModel(contract, readOnly);
			InitializeComponent();
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
	}
}