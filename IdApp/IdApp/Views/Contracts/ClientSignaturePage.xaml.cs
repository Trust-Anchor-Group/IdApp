using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
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
