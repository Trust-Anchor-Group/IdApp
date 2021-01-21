using System.ComponentModel;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
	[DesignTimeVisible(true)]
	public partial class ClientSignaturePage
	{
        private readonly INavigationService navigationService;

		public ClientSignaturePage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            ViewModel = new ClientSignatureViewModel();
			InitializeComponent();
		}

		protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}
