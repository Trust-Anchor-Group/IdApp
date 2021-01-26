using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
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
