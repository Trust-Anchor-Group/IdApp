using System;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels;
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
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
        }

        private void IdentityMenuItem_Clicked(object sender, EventArgs e)
        {
            this.navigationService.GoToAsync("ViewIdentityPage");
        }
    }
}
