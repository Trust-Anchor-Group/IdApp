using IdApp.Services;
using IdApp.ViewModels;
using IdApp.Views;
using IdApp.Views.Contracts;
using IdApp.Views.Registration;
using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp
{
    /// <summary>
    /// The Xamarin Forms Shell implementation of the TAG ID App.
    /// </summary>
    public partial class AppShell
    {
        private readonly INavigationService navigationService;
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IContractOrchestratorService contractOrchestratorService;

        /// <summary>
        /// Create a new instance of the <see cref="AppShell"/> class.
        /// </summary>
        public AppShell()
        {
            this.ViewModel = new AppShellViewModel();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            InitializeComponent();
            SetTabBarIsVisible(this, false);
            RegisterRoutes();
        }

        /// <inheritdoc/>
        protected override void OnNavigated(ShellNavigatedEventArgs e)
        {
            base.OnNavigated(e);
            // Once MainPage is shown, hide the "Loading..." Flyout, as the user shouldn't be
            // able to navigate to that page.
            if (e.Current.Location.ToString().Contains(nameof(MainPage)))
            {
                this.LoadingFlyout.IsVisible = false;
            }
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
            // Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
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
            string code = await QrCode.ScanQrCode(this.navigationService, AppResources.Open);

            if (string.IsNullOrWhiteSpace(code))
                return;

            try
            {
                Uri uri = new Uri(code);

                switch (uri.Scheme.ToLower())
                {
                    case Constants.IoTSchemes.IotId:
                        string legalId = Constants.IoTSchemes.GetCode(code);
                        await this.contractOrchestratorService.OpenLegalIdentity(legalId, AppResources.ScannedQrCode);
                        break;

                    case Constants.IoTSchemes.IotSc:
                        string contractId = Constants.IoTSchemes.GetCode(code);
                        await this.contractOrchestratorService.OpenContract(contractId, AppResources.ScannedQrCode);
                        break;

                    case Constants.IoTSchemes.IotDisco:
                        // TODO handle discovery scheme here.
                        break;

                    default:
                        if (!await Launcher.TryOpenAsync(uri))
                            await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, $"{AppResources.QrCodeNotUnderstood}{Environment.NewLine}{Environment.NewLine}{code}");
                        break;
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
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
