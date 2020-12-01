using System;
using Waher.Networking.XMPP;
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
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly TagProfile tagProfile;
        private readonly INavigationService navigationService;

        public MainPage()
        {
            InitializeComponent();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
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
                            await this.navigationService.DisplayAlert("Error", "Code not understood:\r\n\r\n" + code);
                        break;
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
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
    }
}