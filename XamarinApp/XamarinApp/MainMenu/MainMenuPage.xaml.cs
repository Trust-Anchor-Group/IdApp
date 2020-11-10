using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.MainMenu.Contracts;
using Waher.Networking.XMPP;
using XamarinApp.Services;

namespace XamarinApp.MainMenu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainMenuPage : ContentPage, IConnectionStateChanged, IBackButton
    {
        private readonly ITagService tagService;

        public MainMenuPage()
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.ConnectionStateChanged(this.tagService.Xmpp.State);
        }

        private void Identity_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new IdentityPage(App.CurrentPage), false);
        }

        private void ScanQR_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new ScanQrCodePage(App.CurrentPage), false);
        }

        private void Contracts_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(new ContractsMenuPage(App.CurrentPage), false);
        }

        private void Hide_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
        }

        private void Close_Clicked(object sender, EventArgs e)
        {
            App.Instance?.Stop();
        }

		public void ConnectionStateChanged(XmppState NewState)
		{
            bool Connected = false;

            switch (NewState)
            {
                case XmppState.Authenticating:
                    this.ConnectionState.Text = "Authenticating network identity";
                    break;

                case XmppState.Binding:
                    this.ConnectionState.Text = "Binding connection";
                    break;

                case XmppState.Connected:
                    this.ConnectionState.Text = "Connected to " + (string.IsNullOrEmpty(this.tagService.Xmpp.Domain) ? this.tagService.Xmpp.Host : this.tagService.Xmpp.Domain);
                    Connected = true;
                    break;

                case XmppState.Connecting:
                    this.ConnectionState.Text = "Establishing connection";
                    break;

                case XmppState.Error:
                    this.ConnectionState.Text = "Connection error";
                    break;

                case XmppState.FetchingRoster:
                    this.ConnectionState.Text = "Fetching roster";
                    break;

                case XmppState.Registering:
                    this.ConnectionState.Text = "Registering with broker";
                    break;

                case XmppState.RequestingSession:
                    this.ConnectionState.Text = "Requesting session";
                    break;

                case XmppState.SettingPresence:
                    this.ConnectionState.Text = "Setting presence";
                    break;

                case XmppState.StartingEncryption:
                    this.ConnectionState.Text = "Encrypting connection";
                    break;

                case XmppState.StreamNegotiation:
                    this.ConnectionState.Text = "Negotiating stream";
                    break;

                case XmppState.StreamOpened:
                    this.ConnectionState.Text = "Stream established";
                    break;

                default:
                case XmppState.Offline:
                    this.ConnectionState.Text = "Disconnected";
                    break;
            }

            this.IdentityButton.IsEnabled = Connected;
            this.WalletButton.IsEnabled = false;
            this.ScanQRButton.IsEnabled = Connected;
            this.DevicesButton.IsEnabled = false;
            this.ContractsButton.IsEnabled = Connected;
        }

        public bool BackClicked()
        {
            this.Close_Clicked(this, new EventArgs());
            return true;
        }
    }
}