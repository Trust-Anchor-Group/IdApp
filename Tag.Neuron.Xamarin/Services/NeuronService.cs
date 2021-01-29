using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Tag.Neuron.Xamarin.Extensions;
using Waher.Events.XMPP;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.ServiceDiscovery;

namespace Tag.Neuron.Xamarin.Services
{
    internal sealed class NeuronService : LoadableService, IInternalNeuronService
    {
        private readonly Assembly appAssembly;
        private readonly INetworkService networkService;
        private readonly IInternalLogService logService;
        private readonly ITagProfile tagProfile;
        private Timer reconnectTimer;
        private XmppClient xmppClient;
        private readonly NeuronContracts contracts;
        private readonly NeuronChats chats;
        private string domainName;
        private string accountName;
        private string passwordHash;
        private string passwordHashMethod;
        private bool xmppSettingsOk;
        private readonly InMemorySniffer sniffer;
        private bool isCreatingClient;
        private bool isCreatingContractClient;

        public NeuronService(Assembly appAssembly, ITagProfile tagProfile, IUiDispatcher uiDispatcher, INetworkService networkService, IInternalLogService logService)
        {
            this.appAssembly = appAssembly;
            this.networkService = networkService;
            this.logService = logService;
            this.tagProfile = tagProfile;
            this.contracts = new NeuronContracts(this.tagProfile, uiDispatcher, this, this.logService);
            this.chats = new NeuronChats(this.tagProfile, uiDispatcher, this, this.logService);
            this.sniffer = new InMemorySniffer(250);
            this.tagProfile.StepChanged += TagProfile_StepChanged;
        }

        public void Dispose()
        {
            this.reconnectTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            this.tagProfile.StepChanged -= TagProfile_StepChanged;

            this.DestroyXmppClient();
            this.Contracts.Dispose();
        }

        #region Create/Destroy

        private async Task CreateXmppClient()
        {
            if (isCreatingClient)
                return;

            try
            {
                isCreatingClient = true;

                if (this.xmppClient != null)
                {
                    DestroyXmppClient();
                }
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

                    (string hostName, int portNumber) = await this.networkService.LookupXmppHostnameAndPort(domainName);

                    this.xmppClient = new XmppClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod,
                        Constants.LanguageCodes.Default, appAssembly, this.sniffer)
                    {
                        TrustServer = false,
                        AllowCramMD5 = false,
                        AllowDigestMD5 = false,
                        AllowPlain = false,
                        AllowEncryption = true,
                        AllowScramSHA1 = true,
                        AllowScramSHA256 = true
                    };

                    this.xmppClient.RequestRosterOnStartup = false;
                    this.xmppClient.OnStateChanged += XmppClient_StateChanged;
                    this.xmppClient.OnConnectionError += XmppClient_ConnectionError;
                    this.xmppClient.OnError += XmppClient_Error;

                    this.xmppClient.Connect(domainName);

                    bool connectSucceeded = false;
                    if (this.tagProfile.IsCompleteOrWaitingForValidation())
                    {
                        connectSucceeded = await this.WaitForConnectedState(Constants.Timeouts.XmppConnect);
                    }

                    if (connectSucceeded)
                    {
                        await CreateContractsClientIfNeeded();
                    }
                    else
                    {
                        this.logService.LogWarning("Connect to Xmpp server '{0}' failed for account '{1}' with the specified timeout of {2} ms", 
                            this.domainName,
                            this.accountName,
                            (int)Constants.Timeouts.XmppConnect.TotalMilliseconds);
                    }

                    this.reconnectTimer?.Dispose();
                    this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);
                }
            }
            finally
            {
                isCreatingClient = false;
            }
        }

        private void DestroyXmppClient()
        {
            this.reconnectTimer?.Dispose();
            this.contracts.DestroyClients();
            if (this.xmppClient != null)
            {
                this.xmppClient.OnError -= XmppClient_Error;
                this.xmppClient.OnConnectionError -= XmppClient_ConnectionError;
                this.xmppClient.OnStateChanged -= XmppClient_StateChanged;
                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(XmppState.Offline));
                this.logService.UnRegisterEventSink();
                this.xmppClient.Dispose();
            }
            this.xmppClient = null;
        }

        private bool ShouldCreateClient()
        {
            return this.tagProfile.Step > RegistrationStep.Account &&
                   (this.xmppClient == null ||
                    this.domainName != this.tagProfile.Domain ||
                    this.accountName != this.tagProfile.Account ||
                    this.passwordHash != this.tagProfile.PasswordHash ||
                    this.passwordHashMethod != this.tagProfile.PasswordHashMethod);
        }

        private bool ShouldDestroyClient()
        {
            return this.tagProfile.Step <= RegistrationStep.Account && this.xmppClient != null;
        }

        private async Task CreateContractsClientIfNeeded()
        {
            if (!this.contracts.IsOnline && !this.isCreatingContractClient)
            {
                this.isCreatingContractClient = true;
                try
                {
                    await this.contracts.CreateClients();
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                }
                this.isCreatingContractClient = false;
            }
        }

        #endregion

        private async void TagProfile_StepChanged(object sender, EventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (ShouldCreateClient())
            {
                await this.CreateXmppClient();
            }
            else if (ShouldDestroyClient())
            {
                this.DestroyXmppClient();
            }
        }

        private Task XmppClient_Error(object sender, Exception e)
        {
            this.LatestError = e.Message;
            return Task.CompletedTask;
        }

        private Task XmppClient_ConnectionError(object sender, Exception e)
        {
            this.LatestConnectionError = e.Message;
            return Task.CompletedTask;
        }

        private async Task XmppClient_StateChanged(object sender, XmppState newState)
        {
            switch (newState)
            {
                case XmppState.Connected:
                    this.LatestError = string.Empty;
                    this.LatestConnectionError = string.Empty;

                    this.xmppSettingsOk = true;

                    this.reconnectTimer?.Dispose();
                    this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);

                    if (this.tagProfile.NeedsUpdating())
                    {
                        await this.DiscoverServices();
                    }

                    if (!isCreatingClient)
                    {
                        // Only do this during reconnect attempts.
                        await CreateContractsClientIfNeeded();
                    }

                    this.logService.RegisterEventSink(this.xmppClient, this.tagProfile.LogJid);

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

        public async Task<bool> WaitForConnectedState(TimeSpan timeout)
        {
            if (this.xmppClient == null)
                return false;

            if (this.xmppClient.State == XmppState.Connected)
                return true;

            int i = await this.xmppClient.WaitStateAsync((int)timeout.TotalMilliseconds, XmppState.Connected);
            return i >= 0;
        }

        public override async Task Load(bool isResuming)
        {
            if (this.BeginLoad())
            {
                try
                {
                    if (ShouldCreateClient())
                    {
                        await this.CreateXmppClient();
                    }
                    if (this.xmppClient != null && 
                        this.xmppClient.State == XmppState.Connected && 
                        this.tagProfile.IsCompleteOrWaitingForValidation())
                    {
                        await this.xmppClient.SetPresenceAsync(Availability.Online);
                    }
                    this.EndLoad(true);
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex, new KeyValuePair<string, string>("Method", $"{typeof(NeuronService)}.{nameof(Load)}()"));
                    this.EndLoad(false);
                }
            }
        }

        public override Task Unload()
        {
            return Unload(false);
        }

        public Task UnloadFast()
        {
            return Unload(true);
        }

        private async Task Unload(bool fast)
        {
            if (this.BeginUnload())
            {
                try
                {
                    if (this.xmppClient != null && !fast)
                    {
                        await this.xmppClient.SetPresenceAsync(Availability.Offline);
                    }

                    this.DestroyXmppClient();
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex, new KeyValuePair<string, string>("Method", $"{typeof(NeuronService)}.{nameof(Unload)}()"));
                }

                this.EndUnload();
            }
        }

        private event EventHandler<ConnectionStateChangedEventArgs> ConnectionState;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged
        {
            add
            {
                ConnectionState += value;
                value(this, new ConnectionStateChangedEventArgs(State));
            }
            remove => ConnectionState -= value;
        }

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            ConnectionState?.Invoke(this, e);
        }

        #endregion

        #region State

        public bool IsOnline => !(this.xmppClient is null) && this.xmppClient.State == XmppState.Connected;

        public XmppState State => this.xmppClient?.State ?? XmppState.Offline;

        public string LatestError { get; private set; }

        public string LatestConnectionError { get; private set; }

        public string BareJId => xmppClient?.BareJID ?? string.Empty;

        #endregion

        public INeuronContracts Contracts => this.contracts;
        public INeuronChats Chats => this.chats;

        private enum ConnectOperation
        {
            Connect,
            ConnectAndCreateAccount,
            ConnectAndConnectToAccount
        }

        public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, string hostName, int portNumber, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, string.Empty, string.Empty, languageCode, applicationAssembly, connectedFunc, ConnectOperation.Connect);
        }

        public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, string hostName, int portNumber, string userName, string password,  string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, userName, password, languageCode, applicationAssembly, connectedFunc, ConnectOperation.ConnectAndCreateAccount);
        }

        public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, userName, password, languageCode, applicationAssembly, connectedFunc, ConnectOperation.ConnectAndConnectToAccount);
        }

        private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
        {
            TaskCompletionSource<bool> connected = new TaskCompletionSource<bool>();
            bool succeeded;
            string errorMessage = null;
            bool streamNegotiation = false;
            bool streamOpened = false;
            bool startingEncryption = false;
            bool authenticating = false;
            bool registering = false;
            bool timeout = false;
            string connectionError = null;

            Task OnConnectionError(object _, Exception e)
            {
                connectionError = e.Message;
                connected.TrySetResult(false);
                return Task.CompletedTask;
            }

            async Task OnStateChanged(object _, XmppState newState)
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
                        // When State = Error, wait for the OnConnectionError event to arrive also, as it holds more/direct information.
                        // Just in case it never would - set state error and result.
                        await Task.Delay(Constants.Timeouts.XmppConnect);
                        connected.TrySetResult(false);
                        break;
                }
            }

            try
            {
                using (XmppClient client = new XmppClient(hostName, portNumber, userName, password, languageCode, applicationAssembly, sniffer))
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

                    client.OnConnectionError += OnConnectionError;
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
                    client.OnConnectionError -= OnConnectionError;
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, new KeyValuePair<string, string>(nameof(ConnectOperation), $"{operation}"));
                succeeded = false;
                errorMessage = string.Format(AppResources.UnableToConnectTo, domain);
            }

            if (!succeeded)
            {
                System.Diagnostics.Debug.WriteLine("Sniffer: ", this.sniffer.SnifferToText());

                if (!streamNegotiation || timeout)
                    errorMessage = string.Format(AppResources.CantConnectTo, domain);
                else if (!streamOpened)
                    errorMessage = string.Format(AppResources.DomainIsNotAValidOperator, domain);
                else if (!startingEncryption)
                    errorMessage = string.Format(AppResources.DomainDoesNotFollowEncryptionPolicy, domain);
                else if (!authenticating)
                    errorMessage = string.Format(AppResources.UnableToAuthenticateWith, domain);
                else if (!registering)
                {
                    if (!string.IsNullOrWhiteSpace(connectionError))
                    {
                        errorMessage = connectionError;
                    }
                    else
                    {
                        errorMessage = string.Format(AppResources.OperatorDoesNotSupportRegisteringNewAccounts, domain);
                    }
                }
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
                throw new InvalidOperationException("HttpFileUploadMaxSize is not defined");
            }

            return Task.FromResult(new HttpFileUploadClient(this.xmppClient, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize));
        }

        public Task<MultiUserChatClient> CreateMultiUserChatClientAsync()
        {
            if (this.xmppClient == null)
            {
                throw new InvalidOperationException("XmppClient is not connected");
            }
            if (string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
            {
                throw new InvalidOperationException("MucJid is not defined");
            }

            return Task.FromResult(new MultiUserChatClient(this.xmppClient, this.tagProfile.MucJid));
        }

        public async Task<bool> DiscoverServices(XmppClient client = null)
        {
            client = client ?? xmppClient;
            if (client == null)
                return false;

            ServiceItemsDiscoveryEventArgs response;

            try
            {
                response = await client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                string commsDump = this.sniffer.SnifferToText();
                this.logService.LogException(ex, new KeyValuePair<string, string>("Sniffer", commsDump));
                return false;
            }

            foreach (Item item in response.Items)
            {
                ServiceDiscoveryEventArgs itemResponse = await client.ServiceDiscoveryAsync(null, item.JID, item.Node);

                if (itemResponse.HasFeature(ContractsClient.NamespaceLegalIdentities))
                {
                    this.tagProfile.SetLegalJId(item.JID);
                }

                if (itemResponse.HasFeature(ThingRegistryClient.NamespaceDiscovery))
                {
                    this.tagProfile.SetRegistryJId(item.JID);
                }

                if (itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningDevice) &&
                    itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningOwner) &&
                    itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningToken))
                {
                    this.tagProfile.SetProvisioningJId(item.JID);
                }

                if (itemResponse.HasFeature(HttpFileUploadClient.Namespace))
                {
                    long? maxSize = HttpFileUploadClient.FindMaxFileSize(client, itemResponse);
                    this.tagProfile.SetFileUploadParameters(item.JID, maxSize);
                }

                if (itemResponse.HasFeature(XmppEventSink.NamespaceEventLogging))
                {
                    this.tagProfile.SetLogJId(item.JID);
                }

                if (itemResponse.HasFeature(MultiUserChatClient.NamespaceMuc))
                {
                    this.tagProfile.SetMucJId(item.JID);
                }
            }

            if (string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
                return  false;

            if (string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid) || !this.tagProfile.HttpFileUploadMaxSize.HasValue)
                return false;

            if (string.IsNullOrWhiteSpace(this.tagProfile.LogJid))
                return false;

            if (string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
                return false;

            return true;
        }

        private void ReconnectTimer_Tick(object _)
        {
            if (xmppClient != null &&
                (xmppClient.State == XmppState.Error || xmppClient.State == XmppState.Offline) &&
                this.networkService.IsOnline)
            {
                xmppClient.Reconnect();
            }
        }

        public string CommsDumpAsHtml()
        {
            string html = string.Empty;

            try
            {
                var xslt = new XslCompiledTransform();
                using (Stream xsltStream = this.GetType().Assembly.GetManifestResourceStream($"{typeof(Constants).Namespace}.SnifferXmlToHtml.xslt"))
                using (XmlReader reader = new XmlTextReader(xsltStream))
                {
                    xslt.Load(reader);
                }

                string xml = this.sniffer.SnifferToXml();

                xml = $"<SnifferOutput>{xml}</SnifferOutput>";
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                using (var stream = new MemoryStream())
                using (XmlWriter writer = new XmlTextWriter(stream, Encoding.UTF8))
                {
                    xslt.Transform(doc, null, writer);
                    stream.Position = 0;
                    using (var sr = new StreamReader(stream))
                    { 
                        html = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return html;
        }
    }
}