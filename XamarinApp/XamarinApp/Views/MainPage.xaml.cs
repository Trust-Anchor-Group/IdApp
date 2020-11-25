using System;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly INeuronService neuronService;
        private readonly TagProfile tagProfile;
        private readonly INavigationService navigationService;

        public MainPage()
        {
            InitializeComponent();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
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

        private void Identity_Clicked(object sender, EventArgs e)
        {
 //           this.navigationService.GoTo(new IdentityPage(App.Configuration, App.CurrentPage));
        }

        private void ScanQR_Clicked(object sender, EventArgs e)
        {
 //           this.navigationService.GoTo(new ScanQrCodePage(App.CurrentPage, false), false);
        }

        private void Contracts_Clicked(object sender, EventArgs e)
        {
//            this.navigationService.GoTo(new ContractsMenuPage(App.CurrentPage), false);
        }

        public void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            bool connected = false;

            switch (e.State)
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