using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Waher.IoTGateway.Setup;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Persistence;

namespace XamarinApp.Connection
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class OperatorPage : ContentPage
    {
        private readonly XmppConfiguration xmppConfiguration;

        private string domainName = string.Empty;
        private string hostName = string.Empty;
        private int portNumber = 0;

        public OperatorPage(XmppConfiguration XmppConfiguration)
        {
            this.xmppConfiguration = XmppConfiguration;
            InitializeComponent();

            int Selected = -1;
            int i = 0;

            foreach (string Domain in XmppConfiguration.Domains)
            {
                this.Operators.Items.Add(Domain);

                if (Domain == XmppConfiguration.Domain)
                    Selected = i;

                i++;
            }

            this.Operators.Items.Add("<Other>");

            if (!string.IsNullOrEmpty(XmppConfiguration.Domain))
            {
                this.domainName = XmppConfiguration.Domain;

                if (Selected >= 0)
                    this.Operators.SelectedIndex = Selected;
                else 
                {
                    this.ConnectButton.IsEnabled = true;
                    this.Operators.IsVisible = false;
                    this.Domain.IsVisible = true;
                    this.Domain.Text = XmppConfiguration.Domain;
                    this.Domain.Keyboard = Keyboard.Create(KeyboardFlags.None);
                    this.Domain.Focus();
                }
            }
        }

        private void Operators_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.domainName = string.Empty;

            if (!(this.Operators.SelectedItem is string Item))
                this.ConnectButton.IsEnabled = false;
            else if (Item.StartsWith("<"))
            {
                this.ConnectButton.IsEnabled = false;
                this.Operators.IsVisible = false;
                this.Domain.IsVisible = true;
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        this.Domain.Keyboard = Keyboard.Create(KeyboardFlags.None);
                        this.Domain.Focus();
                    });
                });
            }
            else
            {
                this.domainName = Item;
                this.ConnectButton.IsEnabled = true;
            }
        }

        private async Task<bool> IsValidDomainName(string Name)
        {
            string[] Parts = Name.Split('.');
            if (Parts.Length <= 1)
                return false;

            foreach (string Part in Parts)
            {
                if (string.IsNullOrEmpty(Part))
                    return false;

                foreach (char ch in Part)
                {
                    if (!char.IsLetter(ch) && !char.IsDigit(ch) && ch != '-')
                        return false;
                }
            }

            (string Host, int Port) = await GetXmppClientService(Name);

            if (string.IsNullOrEmpty(Host))
                return false;
            else
            {
                this.domainName = Name;
                this.hostName = Host;
                this.portNumber = Port;
                return true;
            }
        }

        public static async Task<(string, int)> GetXmppClientService(string DomainName)
        {
            try
            {
                SRV SRV = await DnsResolver.LookupServiceEndpoint(DomainName, "xmpp-client", "tcp");
                if (!(SRV is null) && !string.IsNullOrEmpty(SRV.TargetHost) && SRV.Port > 0)
                    return (SRV.TargetHost, SRV.Port);
            }
            catch (Exception)
            {
                // No service endpoint registered
            }

            return (DomainName, 5222);
        }

        private async void Domain_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool Ok;

            try
            {
                Ok = await this.IsValidDomainName(e.NewTextValue);
            }
            catch (Exception)
            {
                Ok = false;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                this.ConnectButton.IsEnabled = Ok;
            });
        }

        private void Connect_Clicked(object sender, EventArgs e)
        {
            this.ConnectButton.IsEnabled = false;
            this.Operators.IsEnabled = false;
            this.Domain.IsEnabled = false;
            this.Connecting.IsRunning = true;

            Task.Run(this.Connect);
        }

        private async Task Connect()
        { 
            try
            {
                (this.hostName, this.portNumber) = await GetXmppClientService(this.domainName);

                InMemorySniffer Sniffer = new InMemorySniffer();

                using (XmppClient Client = new XmppClient(this.hostName, this.portNumber,
                    string.Empty, string.Empty, string.Empty, typeof(App).Assembly, Sniffer))
                {
                    TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();
                    bool StreamNegotiation = false;
                    bool StreamOpened = false;
                    bool StartingEncryption = false;
                    bool Timeout = false;

                    Client.TrustServer = false;
                    Client.AllowCramMD5 = false;
                    Client.AllowDigestMD5 = false;
                    Client.AllowPlain = false;
                    Client.AllowEncryption = true;
                    Client.AllowScramSHA1 = true;

                    Client.OnStateChanged += (sender2, NewState) =>
                    {
                        switch (NewState)
                        {
                            case XmppState.StreamNegotiation:
                                StreamNegotiation = true;
                                break;

                            case XmppState.StreamOpened:
                                StreamOpened = true;
                                break;

                            case XmppState.StartingEncryption:
                                StartingEncryption = true;
                                break;

                            case XmppState.Authenticating:
                                Result.TrySetResult(true);
                                break;

                            case XmppState.Offline:
                            case XmppState.Error:
                                Result.TrySetResult(false);
                                break;
                        }
                    
					    return Task.CompletedTask;
                    };

                    Client.Connect(this.domainName);

                    bool Success;

                    using (Timer Timer = new Timer((P) =>
                    {
                        Timeout = true;
                        Result.TrySetResult(false);
                    }, null, 10000, 0))
                    {
                        Success = await Result.Task;
                    }

                    if (Success)
                    {
                        this.xmppConfiguration.Domain = this.domainName;
                        this.xmppConfiguration.LegalJid = string.Empty;

                        if (this.xmppConfiguration.Step == 0)
                            this.xmppConfiguration.Step++;

                        await Database.Update(this.xmppConfiguration);

                        await App.ShowPage();
                    }
                    else
                    {
                        if (!StreamNegotiation || Timeout)
                            await this.DisplayAlert("Error", "Cannot connect to " + this.domainName, "OK");
                        else if (!StreamOpened)
                            await this.DisplayAlert("Error", this.domainName + " is not a valid operator.", "OK");
                        else if (!StartingEncryption)
                            await this.DisplayAlert("Error", this.domainName + " does not follow the ubiquitous encryption policy.", "OK");
                        else
                            await this.DisplayAlert("Error", "Unable to connect to " + this.domainName, "OK");
                    }
                }
            }
            catch (Exception)
            {
                await this.DisplayAlert("Error", "Unable to connect to " + this.domainName, "OK");
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.Connecting.IsRunning = false;
                    this.ConnectButton.IsEnabled = true;
                    this.Operators.IsEnabled = true;
                    this.Domain.IsEnabled = true;
                });
            }
        }
    }
}
