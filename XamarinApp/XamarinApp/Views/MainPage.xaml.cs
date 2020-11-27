using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.Views.Contracts;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly INeuronService neuronService;
        private readonly IContractsService contractsService;
        private readonly TagProfile tagProfile;
        private readonly INavigationService navigationService;

        public MainPage()
        {
            InitializeComponent();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override void OnDisappearing()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            base.OnDisappearing();
        }

        private async void Identity_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new ViewIdentityPage());
        }

        private async void ScanQR_Clicked(object sender, EventArgs e)
        {
            ScanQrCodePage page = new ScanQrCodePage(true);
            page.Open += ScanQrPage_Open;
            await this.navigationService.PushModalAsync(page);
            string code = await this.OpenQrCode();
            await this.navigationService.PopModalAsync();
            page.Open -= ScanQrPage_Open;

            try
            {
                Uri Uri = new Uri(code);

                switch (Uri.Scheme.ToLower())
                {
                    case "iotid":
                        string LegalId = code.Substring(6);
                        await this.OpenLegalIdentity(LegalId, "Scanned QR Code.");
                        break;

                    case "iotsc":
                        string ContractId = code.Substring(6);
                        await this.OpenContract(ContractId, "Scanned QR Code.");
                        break;

                    case "iotdisco":
                        // TODO
                        break;

                    default:
                        if (!await Launcher.TryOpenAsync(Uri))
                            await this.navigationService.DisplayAlert("Error", "Code not understood:\r\n\r\n" + code);
                        break;
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        TaskCompletionSource<string> openQrCode;

        private Task<string> OpenQrCode()
        {
            openQrCode = new TaskCompletionSource<string>();
            return openQrCode.Task;
        }

        private void ScanQrPage_Open(object sender, OpenEventArgs e)
        {
            if (openQrCode != null)
            {
                openQrCode.TrySetResult(e.Code);
                openQrCode = null;
            }
        }

        private async void Contracts_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new ContractsMenuPage());
        }

        public void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => HandleConnectionStateChanged(e.State));
        }

        public void HandleConnectionStateChanged(XmppState state)
        {
            bool connected = false;

            switch (state)
            {
                case XmppState.Authenticating:
                    this.ConnectionState.Text = AppResources.XmppState_Authenticating;
                    break;

                case XmppState.Binding:
                    this.ConnectionState.Text = AppResources.XmppState_Binding;
                    break;

                case XmppState.Connected:
                    this.ConnectionState.Text = string.Format(AppResources.XmppState_Connected, this.tagProfile.Domain);
                    connected = true;
                    break;

                case XmppState.Connecting:
                    this.ConnectionState.Text = AppResources.XmppState_Connecting;
                    break;

                case XmppState.Error:
                    this.ConnectionState.Text = AppResources.XmppState_Error;
                    break;

                case XmppState.FetchingRoster:
                    this.ConnectionState.Text = AppResources.XmppState_FetchingRoster;
                    break;

                case XmppState.Registering:
                    this.ConnectionState.Text = AppResources.XmppState_Registering;
                    break;

                case XmppState.RequestingSession:
                    this.ConnectionState.Text = AppResources.XmppState_RequestingSession;
                    break;

                case XmppState.SettingPresence:
                    this.ConnectionState.Text = AppResources.XmppState_SettingPresence;
                    break;

                case XmppState.StartingEncryption:
                    this.ConnectionState.Text = AppResources.XmppState_StartingEncryption;
                    break;

                case XmppState.StreamNegotiation:
                    this.ConnectionState.Text = AppResources.XmppState_StreamNegotiation;
                    break;

                case XmppState.StreamOpened:
                    this.ConnectionState.Text = AppResources.XmppState_StreamOpened;
                    break;

                default:
                    this.ConnectionState.Text = AppResources.XmppState_Offline;
                    break;
            }

            this.IdentityButton.IsEnabled = connected;
            this.WalletButton.IsEnabled = false;
            this.ScanQRButton.IsEnabled = connected;
            this.DevicesButton.IsEnabled = false;
            this.ContractsButton.IsEnabled = connected;
        }

        private async Task OpenLegalIdentity(string legalId, string purpose)
        {
            try
            {
                LegalIdentity identity = await this.contractsService.GetLegalIdentityAsync(legalId);
                await this.navigationService.PushAsync(new ViewIdentityPage(identity));
            }
            catch (Exception)
            {
                await this.contractsService.PetitionIdentityAsync(legalId, Guid.NewGuid().ToString(), purpose);
                await this.navigationService.DisplayAlert("Petition Sent", "A petition has been sent to the owner of the identity. " +
                    "If the owner accepts the petition, the identity information will be displayed on the screen.", AppResources.Ok);
            }
        }

        protected async Task OpenContract(string contractId, string purpose)
        {
            try
            {
                Contract contract = await this.contractsService.GetContractAsync(contractId);

                if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
                {
                    await this.navigationService.PushAsync(new NewContractPage(contract));
                }
                else
                {
                    await this.navigationService.PushAsync(new ViewContractPage(contract, false));
                }
            }
            catch (Exception)
            {
                await this.contractsService.PetitionContractAsync(contractId, Guid.NewGuid().ToString(), purpose);
                await this.navigationService.DisplayAlert("Petition Sent", "A petition has been sent to the parts of the contract. " +
                    "If any of the parts accepts the petition, the contract information will be displayed on the screen.", AppResources.Ok);
            }
        }

    }
}