using System.ComponentModel;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
	[DesignTimeVisible(true)]
	public partial class ClientSignaturePage
	{
        private readonly INavigationService navigationService;

		public ClientSignaturePage(ClientSignature signature, LegalIdentity identity)
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            ViewModel = new ClientSignatureViewModel(signature, identity);
			InitializeComponent();
		}

		protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
    }
}
