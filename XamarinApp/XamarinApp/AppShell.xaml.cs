using System;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels;
using XamarinApp.Views;
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
            InitializeComponent();
            SetTabBarIsVisible(this, false);
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
            Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
        }

        private async void IdentityMenuItem_Clicked(object sender, EventArgs e)
        {
            string route = "ViewIdentityPage";
            Current.FlyoutIsPresented = false;
            await this.navigationService.GoToAsync(route);
        }
    }
}
