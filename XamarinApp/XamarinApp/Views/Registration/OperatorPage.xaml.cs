using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Waher.IoTGateway.Setup;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using XamarinApp.Services;

namespace XamarinApp.Views.Registration
{
    [DesignTimeVisible(true)]
    public partial class OperatorPage : ContentPage
    {
        private readonly ITagService tagService;
        private string domainName = string.Empty;
        private string hostName = string.Empty;
        private int portNumber = 0;

        public OperatorPage()
        {
            InitializeComponent();

            this.tagService = DependencyService.Resolve<ITagService>();

            int Selected = -1;
            int i = 0;

            foreach (string Domain in XmppConfiguration.Domains)
            {
                this.Operators.Items.Add(Domain);

                if (Domain == this.tagService.Configuration.Domain)
                    Selected = i;

                i++;
            }

            this.Operators.Items.Add("<Other>");

            if (!string.IsNullOrEmpty(this.tagService.Configuration.Domain))
            {
                this.domainName = this.tagService.Configuration.Domain;

                if (Selected >= 0)
                    this.Operators.SelectedIndex = Selected;
                else 
                {
                    this.ConnectButton.IsEnabled = true;
                    this.Operators.IsVisible = false;
                    this.Domain.IsVisible = true;
                    this.Domain.Text = this.tagService.Configuration.Domain;
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

            (string Host, int Port) = await this.tagService.GetXmppHostnameAndPort(Name);

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
                (this.hostName, this.portNumber) = await this.tagService.GetXmppHostnameAndPort(this.domainName);

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
                        this.tagService.Configuration.Domain = this.domainName;
                        this.tagService.Configuration.LegalJid = string.Empty;

                        if (this.tagService.Configuration.Step == 0)
                            this.tagService.Configuration.Step++;

                        this.tagService.UpdateConfiguration();

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
