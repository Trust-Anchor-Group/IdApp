using System;
using System.Threading.Tasks;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels;
using XamarinApp.Views;
using XamarinApp.Views.Contracts;
using XamarinApp.Views.Registration;

namespace XamarinApp
{
    public partial class AppShell
    {
        private readonly INavigationService navigationService;

        public AppShell()
        {
            this.ViewModel = new AppShellViewModel();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                
            }
            SetTabBarIsVisible(this, false);
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
            Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
            Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
            Routing.RegisterRoute(nameof(MyContractsPage), typeof(MyContractsPage));
            Routing.RegisterRoute(nameof(SignedContractsPage), typeof(SignedContractsPage));
            Routing.RegisterRoute(nameof(NewContractPage), typeof(NewContractPage));
            Routing.RegisterRoute(nameof(ViewContractPage), typeof(ViewContractPage));
            Routing.RegisterRoute(nameof(ClientSignaturePage), typeof(ClientSignaturePage));
            Routing.RegisterRoute(nameof(ServerSignaturePage), typeof(ServerSignaturePage));
            Routing.RegisterRoute(nameof(PetitionContractPage), typeof(PetitionContractPage));
            Routing.RegisterRoute(nameof(PetitionIdentityPage), typeof(PetitionIdentityPage));
            Routing.RegisterRoute(nameof(XmppCommunicationPage), typeof(XmppCommunicationPage));
        }

        private async Task GoToPage(string route)
        {
            // Due to a bug in Xamarin.Forms 4.8 the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
            // Therefore we have to close it manually here.
            Current.FlyoutIsPresented = false;
            await this.navigationService.GoToAsync(route);
        }

        private async void ViewIdentityMenuItem_Clicked(object sender, EventArgs e)
        {
            await this.GoToPage(nameof(ViewIdentityPage));
        }

        private async void ScanQrCodeMenuItem_Clicked(object sender, EventArgs e)
        {
            await this.GoToPage(nameof(ScanQrCodePage));
        }

        private async void MyContractsMenuItem_Clicked(object sender, EventArgs e)
        {
            await this.GoToPage(nameof(MyContractsPage));
        }

        private async void SignedContractsMenuItem_Clicked(object sender, EventArgs e)
        {
            await this.GoToPage(nameof(SignedContractsPage));
        }

        private async void NewContractMenuItem_Clicked(object sender, EventArgs e)
        {
            await this.GoToPage(nameof(NewContractPage));
        }

        private async void DebugMenuItem_Clicked(object sender, EventArgs e)
        {
            await this.GoToPage(nameof(XmppCommunicationPage));
        }
    }
}
