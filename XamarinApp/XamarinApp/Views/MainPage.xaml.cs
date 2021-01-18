using System;
using System.Collections.Generic;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Navigation;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using XamarinApp.Views.Contracts;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly INavigationService navigationService;

        private static readonly SortedDictionary<string, SortedDictionary<string, string>> ContractTypesPerCategory =
            new SortedDictionary<string, SortedDictionary<string, string>>()
            {/*
                {
                    "Put Title of Contract Category here",
                    new SortedDictionary<string, string>()
                    {
                        { "Put Title of Contract Template here", "Put contract identity of template here." }
                    }
                }*/
            };

        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        private async void ViewIdentityButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new ViewIdentityPage());
        }

        private async void ScanQrCodeButton_Clicked(object sender, EventArgs e)
        {
            ScanQrCodePage page = new ScanQrCodePage();
            string code = await page.ScanQrCode();

            if (string.IsNullOrWhiteSpace(code))
                return;

            try
            {
                Uri uri = new Uri(code);

                switch (uri.Scheme.ToLower())
                {
                    case Constants.IoTSchemes.IotId:
                        string legalId = Constants.IoTSchemes.GetCode(code);
                        await this.contractOrchestratorService.OpenLegalIdentity(legalId, "Scanned QR Code");
                        break;

                    case Constants.IoTSchemes.IotSc:
                        string contractId = Constants.IoTSchemes.GetCode(code);
                        await this.contractOrchestratorService.OpenContract(contractId, "Scanned QR Code");
                        break;

                    case Constants.IoTSchemes.IotDisco:
                        // TODO handle discovery scheme here.
                        break;

                    default:
                        if (!await Launcher.TryOpenAsync(uri))
                            await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, $"Code not understood:{Environment.NewLine}{Environment.NewLine}" + code);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async void CreatedContractsButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new MyContractsPage());
        }

        private async void SignedContractsButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new SignedContractsPage());
        }

        private async void NewContractButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new NewContractPage(), new NewContractNavigationArgs(ContractTypesPerCategory));
        }

        private async void XmppCommunicationButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new XmppCommunicationPage());
        }
    }
}