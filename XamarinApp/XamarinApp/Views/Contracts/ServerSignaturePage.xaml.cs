using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [DesignTimeVisible(true)]
	public partial class ServerSignaturePage
	{
        private readonly INavigationService navigationService;

		public ServerSignaturePage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            ViewModel = new ServerSignatureViewModel();
			InitializeComponent();
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
	}
}
