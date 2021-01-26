using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    [DesignTimeVisible(true)]
    public partial class PetitionIdentityPage
    {
        private readonly INavigationService navigationService;

        public PetitionIdentityPage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new PetitionIdentityViewModel();
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}
