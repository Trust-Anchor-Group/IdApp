using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.ServiceDiscovery;
using XamarinApp.Extensions;

namespace XamarinApp.Services
{
    internal sealed class NeuronService : LoadableService, INeuronService
    {
        private readonly TagProfile tagProfile;
        private Timer reconnectTimer = null;
        private XmppClient xmppClient = null;
        private string domainName = null;
        private string accountName = null;
        private string passwordHash = null;
        private string passwordHashMethod = null;
        private bool xmppSettingsOk = false;
        private readonly ISniffer sniffer;

        public NeuronService(TagProfile tagProfile)
        {
            this.tagProfile = tagProfile;
            this.sniffer = new InMemorySniffer(250);
            this.tagProfile.StepChanged += TagProfile_StepChanged;
        }

        public void Dispose()
        {
            this.reconnectTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            this.tagProfile.StepChanged -= TagProfile_StepChanged;

            this.DestroyXmppClient();
        }

        #region Create/Destroy

        private async Task CreateXmppClient()
        {
            if (this.xmppClient is null ||
                this.domainName != this.tagProfile.Domain ||
                this.accountName != this.tagProfile.Account ||
                this.passwordHash != this.tagProfile.PasswordHash ||
                this.passwordHashMethod != this.tagProfile.PasswordHashMethod)
            {
                this.domainName = this.tagProfile.Domain;
                this.accountName = this.tagProfile.Account;
                this.passwordHash = this.tagProfile.PasswordHash;
                this.passwordHashMethod = this.tagProfile.PasswordHashMethod;

                (string hostName, int portNumber) = await this.tagProfile.GetXmppHostnameAndPort(domainName);

                this.xmppClient = new XmppClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod, Constants.LanguageCodes.Default, typeof(App).Assembly, this.sniffer);
                this.xmppClient.TrustServer = false;
                this.xmppClient.AllowCramMD5 = false;
                this.xmppClient.AllowDigestMD5 = false;
                this.xmppClient.AllowPlain = false;
                this.xmppClient.AllowEncryption = true;
                this.xmppClient.AllowScramSHA1 = true;
                this.xmppClient.AllowScramSHA256 = true;

                this.xmppClient.OnStateChanged += XmppClient_StateChanged;

                this.xmppClient.Connect(domainName);

                this.reconnectTimer?.Dispose();
                this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);
            }
        }

        private void DestroyXmppClient()
        {
            this.reconnectTimer?.Dispose();
            if (this.xmppClient != null)
            {
                this.xmppClient.OnStateChanged -= XmppClient_StateChanged;
                this.xmppClient.Dispose();
            }
            this.xmppClient = null;
        }

        private bool ShouldCreateClient()
        {
            return this.tagProfile.Step > RegistrationStep.Account && this.xmppClient == null;
        }

        private bool ShouldDestroyClient()
        {
            return this.tagProfile.Step <= RegistrationStep.Account && this.xmppClient != null;
        }

        #endregion

        private async void TagProfile_StepChanged(object sender, EventArgs e)
        {
            if (ShouldCreateClient())
            {
                await this.CreateXmppClient();
            }
            else if (ShouldDestroyClient())
            {
                this.DestroyXmppClient();
            }
        }

        private async Task XmppClient_StateChanged(object sender, XmppState newState)
        {
            switch (newState)
            {
                case XmppState.Connected:
                    this.xmppSettingsOk = true;

                    this.reconnectTimer?.Dispose();
                    this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);

                    if (string.IsNullOrEmpty(this.tagProfile.LegalJid) ||
                        string.IsNullOrEmpty(this.tagProfile.RegistryJid) ||
                        string.IsNullOrEmpty(this.tagProfile.ProvisioningJid) ||
                        string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid))
                    {
                        await this.DiscoverServices();
                    }
                    break;

                case XmppState.Error:
                    if (this.xmppSettingsOk)
                    {
                        this.xmppSettingsOk = false;
                        this.xmppClient?.Reconnect();
                    }
                    break;
            }

            this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(newState));
        }

        #region Lifecycle

        public override async Task Load()
        {
            if (!this.IsLoaded && !this.IsLoading)
            {
                this.BeginLoad();

                if (ShouldCreateClient())
                {
                    await this.CreateXmppClient();
                }
                this.xmppClient?.SetPresence(Availability.Online);

                this.EndLoad(true);
            }
        }

        public override async Task Unload()
        {
            this.BeginUnload();

            if (this.xmppClient != null)
            {
                TaskCompletionSource<bool> offlineSent = new TaskCompletionSource<bool>();
                this.xmppClient.SetPresence(Availability.Offline, (sender, e) => offlineSent.TrySetResult(true));
                Task _ = Task.Delay(Constants.Timeouts.XmppPresence).ContinueWith(__ => offlineSent.TrySetResult(false));

                await offlineSent.Task;
            }

            this.DestroyXmppClient();

            this.EndUnload();
        }

        private event EventHandler<ConnectionStateChangedEventArgs> connectionState;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged
        {
            add
            {
                connectionState += value;
                value(this, new ConnectionStateChangedEventArgs(State));
            }
            remove
            {
                connectionState -= value;
            }
        }

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            connectionState?.Invoke(this, e);
        }

        #endregion

        #region State

        public bool IsOnline => !(xmppClient is null) && xmppClient.State == XmppState.Connected;

        public XmppState State => xmppClient?.State ?? XmppState.Offline;

        public string BareJId => xmppClient?.BareJID ?? string.Empty;

        #endregion

        private enum ConnectOperation
        {
            Connect,
            ConnectAndCreateAccount,
            ConnectAndConnectToAccount
        }

        public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, string hostName, int portNumber, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, string.Empty, string.Empty, languageCode, appAssembly, connectedFunc, ConnectOperation.Connect);
        }

        public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, string hostName, int portNumber, string userName, string password,  string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, userName, password, languageCode, appAssembly, connectedFunc, ConnectOperation.ConnectAndCreateAccount);
        }

        public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, userName, password, languageCode, appAssembly, connectedFunc, ConnectOperation.ConnectAndConnectToAccount);
        }

        private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
        {
            TaskCompletionSource<bool> connected = new TaskCompletionSource<bool>();
            bool succeeded = false;
            string errorMessage = null;
            bool streamNegotiation = false;
            bool streamOpened = false;
            bool startingEncryption = false;
            bool authenticating = false;
            bool registering = false;
            bool timeout = false;

            Task OnStateChanged(object _, XmppState newState)
            {
                switch (newState)
                {
                    case XmppState.StreamNegotiation:
                        streamNegotiation = true;
                        break;

                    case XmppState.StreamOpened:
                        streamOpened = true;
                        break;

                    case XmppState.StartingEncryption:
                        startingEncryption = true;
                        break;

                    case XmppState.Authenticating:
                        authenticating = true;
                        if (operation == ConnectOperation.Connect)
                        {
                            connected.TrySetResult(true);
                        }
                        break;

                    case XmppState.Registering:
                        registering = true;
                        break;

                    case XmppState.Connected:
                        connected.TrySetResult(true);
                        break;

                    case XmppState.Offline:
                        connected.TrySetResult(false);
                        break;

                    case XmppState.Error:
                        if (operation == ConnectOperation.ConnectAndCreateAccount)
                            connected.TrySetResult(true);
                        else
                            connected.TrySetResult(false);
                        break;
                }

                return Task.CompletedTask;
            }

            try
            {
                using (XmppClient client = new XmppClient(hostName, portNumber, userName, password, languageCode, appAssembly, sniffer))
                {
                    if (operation == ConnectOperation.ConnectAndCreateAccount)
                    {
                        if (this.tagProfile.TryGetKeys(domain, out string key, out string secret))
                            client.AllowRegistration(key, secret);
                        else
                            client.AllowRegistration();
                    }

                    client.TrustServer = false;
                    client.AllowCramMD5 = false;
                    client.AllowDigestMD5 = false;
                    client.AllowPlain = false;
                    client.AllowEncryption = true;
                    client.AllowScramSHA1 = true;

                    client.OnStateChanged += OnStateChanged;

                    client.Connect(domain);

                    void TimerCallback(object _)
                    {
                        timeout = true;
                        connected.TrySetResult(false);
                    }

                    using (Timer _ = new Timer(TimerCallback, null, (int)Constants.Timeouts.XmppConnect.TotalMilliseconds, Timeout.Infinite))
                    {
                        succeeded = await connected.Task;
                    }

                    if (succeeded && connectedFunc != null)
                    {
                        await connectedFunc(client);
                    }

                    client.OnStateChanged -= OnStateChanged;
                }
            }
            catch (Exception)
            {
                succeeded = false;
                errorMessage = string.Format(AppResources.UnableToConnectTo, domain);
            }

            if (!succeeded)
            {
                System.Diagnostics.Debug.WriteLine("Sniffer: ", ((InMemorySniffer)this.sniffer).SnifferToText());

                if (!streamNegotiation || timeout)
                    errorMessage = string.Format(AppResources.CantConnectTo, domain);
                else if (!streamOpened)
                    errorMessage = string.Format(AppResources.DomainIsNotAValidOperator, domain);
                else if (!startingEncryption)
                    errorMessage = string.Format(AppResources.DomainDoesNotFollowEncryptionPolicy, domain);
                else if (!authenticating)
                    errorMessage = string.Format(AppResources.UnableToAuthenticateWith, domain);
                else if (!registering)
                    errorMessage = string.Format(AppResources.OperatorDoesNotSupportRegisteringNewAccounts, domain);
                else if (operation == ConnectOperation.ConnectAndCreateAccount)
                    errorMessage = string.Format(AppResources.AccountNameAlreadyTaken, accountName);
                else if (operation == ConnectOperation.ConnectAndConnectToAccount)
                    errorMessage = string.Format(AppResources.InvalidUsernameOrPassword, accountName);
                else
                    errorMessage = string.Format(AppResources.UnableToConnectTo, domain);
            }

            return (succeeded, errorMessage);
        }

        public Task<ContractsClient> CreateContractsClientAsync()
        {
            if (this.xmppClient == null)
            {
                throw new InvalidOperationException("XmppClient is not connected");
            }
            if (string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
            {
                throw new InvalidOperationException("LegalJid is not defined");
            }

            return ContractsClient.Create(this.xmppClient, this.tagProfile.LegalJid);
        }


        public Task<HttpFileUploadClient> CreateFileUploadClientAsync()
        {
            if (this.xmppClient == null)
            {
                throw new InvalidOperationException("XmppClient is not connected");
            }
            if (string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid))
            {
                throw new InvalidOperationException("HttpFileUploadJid is not defined");
            }
            if (!this.tagProfile.HttpFileUploadMaxSize.HasValue)
            {
                throw new InvalidOperationException("HttpFileUploadMaxSize is missing");
            }

            return Task.FromResult(new HttpFileUploadClient(this.xmppClient, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize));
        }

        public async Task<bool> DiscoverServices(XmppClient client = null)
        {
            client = client ?? xmppClient;
            if (client == null)
                return false;

            ServiceItemsDiscoveryEventArgs e2 = await client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);

            foreach (Item Item in e2.Items)
            {
                ServiceDiscoveryEventArgs e3 = await client.ServiceDiscoveryAsync(null, Item.JID, Item.Node);

                if (e3.HasFeature(ContractsClient.NamespaceLegalIdentities) &&
                    e3.HasFeature(ContractsClient.NamespaceLegalIdentities))
                {
                    this.tagProfile.SetLegalJId(Item.JID);
                }

                if (e3.HasFeature(ThingRegistryClient.NamespaceDiscovery))
                {
                    this.tagProfile.SetRegistryJId(Item.JID);
                }

                if (e3.HasFeature(ProvisioningClient.NamespaceProvisioningDevice) &&
                    e3.HasFeature(ProvisioningClient.NamespaceProvisioningOwner) &&
                    e3.HasFeature(ProvisioningClient.NamespaceProvisioningToken))
                {
                    this.tagProfile.SetProvisioningJId(Item.JID);
                }

                if (e3.HasFeature(HttpFileUploadClient.Namespace))
                {
                    long? maxSize = HttpFileUploadClient.FindMaxFileSize(client, e3);
                    this.tagProfile.SetHttpParameters(Item.JID, maxSize);
                }
            }

            if (string.IsNullOrEmpty(this.tagProfile.LegalJid))
                return  false;

            if (string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid) || !this.tagProfile.HttpFileUploadMaxSize.HasValue)
                return false;

            return true;
        }

        private void ReconnectTimer_Tick(object _)
        {
            if (!(xmppClient is null) && (xmppClient.State == XmppState.Error || xmppClient.State == XmppState.Offline))
                xmppClient.Reconnect();
        }
    }
}