using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using XamarinApp.Views.Contracts;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly TagProfile tagProfile;
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
            this.tagProfile = DependencyService.Resolve<TagProfile>();
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
                        string legalId = code.Substring(6);
                        await this.contractOrchestratorService.OpenLegalIdentity(legalId, "Scanned QR Code.");
                        break;

                    case Constants.IoTSchemes.IotSc:
                        string contractId = code.Substring(6);
                        await this.contractOrchestratorService.OpenContract(contractId, "Scanned QR Code.");
                        break;

                    case Constants.IoTSchemes.IotDisco:
                        // TODO
                        break;

                    default:
                        if (!await Launcher.TryOpenAsync(uri))
                            await this.navigationService.DisplayAlert(AppResources.ErrorTitle, $"Code not understood:{Environment.NewLine}{Environment.NewLine}" + code);
                        break;
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private async void CreatedContractsButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new MyContractsPage(true));
        }

        private async void SignedContractsButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new MyContractsPage(false));
        }

        private async void NewContractButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new NewContractPage(ContractTypesPerCategory));
        }
    }
}