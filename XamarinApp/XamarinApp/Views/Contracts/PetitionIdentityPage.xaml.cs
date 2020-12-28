using System.ComponentModel;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
    public partial class PetitionIdentityPage
    {
        private readonly INavigationService navigationService;

        public PetitionIdentityPage(
            LegalIdentity requestorIdentity, 
            string requestorFullJid,
            string requestedIdentityId, 
            string petitionId,
            string purpose)
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new PetitionIdentityViewModel(requestorIdentity, requestorFullJid, requestedIdentityId, petitionId, purpose);
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
    }
}
