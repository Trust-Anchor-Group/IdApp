using System.ComponentModel;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
	public partial class PetitionContractPage
	{
        private readonly INavigationService navigationService;

		public PetitionContractPage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new PetitionContractViewModel();
			InitializeComponent();

            // TODO: fix this. Is it a simple copy/paste to get certain sections into a page?
   //         ViewContractPage info = new ViewContractPage(requestedContract, true);
			//info.MoveInfo(this.TableView);
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
	}
}
