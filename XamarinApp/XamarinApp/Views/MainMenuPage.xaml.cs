using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP;
using XamarinApp.Services;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainMenuPage
    {
        private readonly ITagService tagService;

        public MainMenuPage()
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.tagService.ConnectionStateChanged += ConnectionStateChanged;
        }

        protected override void OnDisappearing()
        {
            this.tagService.ConnectionStateChanged -= ConnectionStateChanged;
            base.OnDisappearing();
        }

        private void Identity_Clicked(object sender, EventArgs e)
        {
            //App.ShowPage(new IdentityPage(App.CurrentPage), false);
        }

        private void ScanQR_Clicked(object sender, EventArgs e)
        {
            //App.ShowPage(new ScanQrCodePage(App.CurrentPage, false), false);
        }

        private void Contracts_Clicked(object sender, EventArgs e)
        {
            //App.ShowPage(new Contracts.ContractsMenuPage(App.CurrentPage), false);
        }

        private void ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => HandleConnectionStateChanged(e.State));
        }

        private void HandleConnectionStateChanged(XmppState state)
		{
            bool Connected = false;

            switch (state)
            {
                case XmppState.Authenticating:
                    this.ConnectionState.Text = AppResources.XmppState_Authenticating;
                    break;

                case XmppState.Binding:
                    this.ConnectionState.Text = AppResources.XmppState_Binding;
                    break;

                case XmppState.Connected:
                    this.ConnectionState.Text = string.Format(AppResources.XmppState_Connected, (string.IsNullOrEmpty(this.tagService.Domain) ? this.tagService.Host : this.tagService.Domain));
                    Connected = true;
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
                case XmppState.Offline:
                    this.ConnectionState.Text = AppResources.XmppState_Offline;
                    break;
            }

            this.IdentityButton.IsEnabled = Connected;
            this.WalletButton.IsEnabled = false;
            this.ScanQRButton.IsEnabled = Connected;
            this.DevicesButton.IsEnabled = false;
            this.ContractsButton.IsEnabled = Connected;
        }
    }
}