using System.ComponentModel;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
	public partial class ServerSignaturePage
	{
        private readonly INavigationService navigationService;

		public ServerSignaturePage(Contract contract)
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            ViewModel = new ServerSignatureViewModel(contract);
			InitializeComponent();
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
	}
}
