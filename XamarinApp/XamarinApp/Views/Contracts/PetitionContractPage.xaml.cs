using System.ComponentModel;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
	public partial class PetitionContractPage
	{
        private readonly INavigationService navigationService;

		public PetitionContractPage(
            LegalIdentity requestorIdentity, 
            string requestorFullJid,
			Contract requestedContract, 
            string petitionId, 
            string purpose)
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new PetitionContractViewModel(requestorIdentity, requestorFullJid, requestedContract, petitionId, purpose);
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
