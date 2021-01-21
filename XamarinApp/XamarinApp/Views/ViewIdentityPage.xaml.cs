using System.ComponentModel;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views
{
    [DesignTimeVisible(true)]
    public partial class ViewIdentityPage
    {
        private readonly INavigationService navigationService;

        public ViewIdentityPage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new ViewIdentityViewModel(
                DependencyService.Resolve<ITagProfile>(),
                DependencyService.Resolve<IUiDispatcher>(),
                DependencyService.Resolve<INeuronService>(),
                this.navigationService,
                DependencyService.Resolve<INetworkService>(),
                DependencyService.Resolve<ILogService>());
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}
