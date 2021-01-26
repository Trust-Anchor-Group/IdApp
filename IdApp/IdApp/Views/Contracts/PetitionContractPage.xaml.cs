using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
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
            this.navigationService.GoBackAsync();
            return true;
        }
	}
}
