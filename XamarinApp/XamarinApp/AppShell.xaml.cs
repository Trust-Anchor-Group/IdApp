using System;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.ViewModels;

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
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await this.navigationService.ReplaceAsync("LoginPage");
        }
    }
}
