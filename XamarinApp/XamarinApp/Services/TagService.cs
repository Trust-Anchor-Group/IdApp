using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Xamarin.Forms;
using Waher.Networking.DNS;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Runtime.Settings;
using Waher.Runtime.Temporary;

namespace XamarinApp.Services
{
    internal sealed class TagService : LoadableService, ITagService
    {
        private static readonly TimeSpan FileUploadTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ReconnectInterval = TimeSpan.FromMinutes(1);

        private readonly TagProfile tagProfile;
        private Timer reconnectTimer = null;
        private XmppClient xmppClient = null;
        private ContractsClient contractsClient = null;
        private HttpFileUploadClient fileUploadClient = null;
        private string domainName = null;
        private string accountName = null;
        private string passwordHash = null;
        private string passwordHashMethod = null;
        private bool xmppSettingsOk = false;
        private readonly IMessageService messageService;
        private InternalSink internalSink;
        private readonly ISniffer sniffer;

        public TagService(TagProfile tagSettings, IMessageService messageService)
        {
            this.tagProfile = tagSettings;
            this.messageService = messageService;
            this.sniffer = new InMemorySniffer(250);
            this.tagProfile.StepChanged += TagProfile_StepChanged;
        }

        public void Dispose()
        {
            this.reconnectTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            this.tagProfile.StepChanged -= TagProfile_StepChanged;

            this.DestroyFileUploadClient();
            this.DestroyContractsClient();
            this.DestroyXmppClient();
        }

        private async Task CreateContractsClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid) && !(xmppClient is null))
            {
                this.contractsClient = await ContractsClient.Create(xmppClient, this.tagProfile.LegalJid);
                this.contractsClient.IdentityUpdated += ContractsClient_IdentityUpdated;
                this.contractsClient.PetitionForIdentityReceived += ContractsClient_PetitionForIdentityReceived;
                this.contractsClient.PetitionedIdentityResponseReceived += ContractsClient_PetitionedIdentityResponseReceived;
                this.contractsClient.PetitionForContractReceived += ContractsClient_PetitionForContractReceived;
                this.contractsClient.PetitionedContractResponseReceived += ContractsClient_PetitionedContractResponseReceived;
                this.contractsClient.PetitionForSignatureReceived += ContractsClient_PetitionForSignatureReceived;
                this.contractsClient.PetitionedSignatureResponseReceived += ContractsClient_PetitionedSignatureResponseReceived;
                this.contractsClient.PetitionForPeerReviewIDReceived += ContractsClient_PetitionForPeerReviewIDReceived;
                this.contractsClient.PetitionedPeerReviewIDResponseReceived += ContractsClient_PetitionedPeerReviewIDResponseReceived;
            }
        }

        private void DestroyContractsClient()
        {
            if (this.contractsClient != null)
            {
                this.contractsClient.IdentityUpdated -= ContractsClient_IdentityUpdated;
                this.contractsClient.PetitionForIdentityReceived -= ContractsClient_PetitionForIdentityReceived;
                this.contractsClient.PetitionedIdentityResponseReceived -= ContractsClient_PetitionedIdentityResponseReceived;
                this.contractsClient.PetitionForContractReceived -= ContractsClient_PetitionForContractReceived;
                this.contractsClient.PetitionedContractResponseReceived -= ContractsClient_PetitionedContractResponseReceived;
                this.contractsClient.PetitionForSignatureReceived -= ContractsClient_PetitionForSignatureReceived;
                this.contractsClient.PetitionedSignatureResponseReceived -= ContractsClient_PetitionedSignatureResponseReceived;
                this.contractsClient.PetitionForPeerReviewIDReceived -= ContractsClient_PetitionForPeerReviewIDReceived;
                this.contractsClient.PetitionedPeerReviewIDResponseReceived -= ContractsClient_PetitionedPeerReviewIDResponseReceived;
                this.contractsClient.Dispose();
            }
        }

        private void CreateFileUploadClient()
        {
            if (!string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid) && this.tagProfile.HttpFileUploadMaxSize.HasValue && !(this.xmppClient is null))
            {
                this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize.Value);
            }
        }

        private void DestroyFileUploadClient()
        {
            fileUploadClient?.Dispose();
            fileUploadClient = null;
        }

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
                this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, ReconnectInterval, ReconnectInterval);
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

        private async void TagProfile_StepChanged(object sender, EventArgs e)
        {
            if (this.tagProfile.Step >= RegistrationStep.Account && this.xmppClient == null)
            {
                await this.CreateXmppClient();
                await this.CreateContractsClient();
                this.CreateFileUploadClient();
            }
            else if (this.tagProfile.Step < RegistrationStep.Account && this.xmppClient != null)
            {
                this.DestroyFileUploadClient();
                this.DestroyContractsClient();
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
                    this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, ReconnectInterval, ReconnectInterval);

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

                this.internalSink = new InternalSink();
                Log.Register(this.internalSink);

                try
                {
                    Types.Initialize(
                        typeof(App).Assembly,
                        typeof(Database).Assembly,
                        typeof(FilesProvider).Assembly,
                        typeof(ObjectSerializer).Assembly,
                        typeof(XmppClient).Assembly,
                        typeof(ContractsClient).Assembly,
                        typeof(RuntimeSettings).Assembly,
                        typeof(Language).Assembly,
                        typeof(DnsResolver).Assembly,
                        typeof(XmppServerlessMessaging).Assembly,
                        typeof(ProvisioningClient).Assembly);
                    await Types.StartAllModules((int)TimeSpan.FromMilliseconds(1000).TotalMilliseconds);
                }
                catch (Exception e)
                {
                    await this.messageService.DisplayAlert(AppResources.ErrorTitle, e.ToString(), AppResources.Ok);
                    this.EndLoad(false);
                    return;
                }

                this.xmppClient?.SetPresence(Availability.Online);

                this.EndLoad(true);
            }
        }

        public override async Task Unload()
        {
            this.BeginUnload();

            if (!(this.xmppClient is null))
            {
                TaskCompletionSource<bool> offlineSent = new TaskCompletionSource<bool>();

                this.xmppClient.SetPresence(Availability.Offline, (sender, e) => offlineSent.TrySetResult(true));
                Task _ = Task.Delay(1000).ContinueWith(__ => offlineSent.TrySetResult(false));

                await offlineSent.Task;
            }

            this.DestroyFileUploadClient();
            this.DestroyContractsClient();
            this.DestroyXmppClient();

            await DatabaseModule.Flush();
            await Types.StopAllModules();
            Log.Unregister(this.internalSink);
            Log.Terminate();
            this.internalSink.Dispose();
            this.internalSink = null;

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
        //public string Domain => xmpp?.Domain ?? string.Empty;
        //public string Account => tagProfile.Account ?? string.Empty;
        //public string Host => xmpp?.Host ?? string.Empty;
        public string BareJId => xmppClient?.BareJID ?? string.Empty;

        #endregion

        private enum ConnectOperation
        {
            Connect,
            ConnectAndCreateAccount,
            ConnectAndConnectToAccount
        }

        public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, accountName, passwordHash, passwordHashMethod, languageCode, appAssembly, connectedFunc, ConnectOperation.Connect);
        }

        public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, accountName, passwordHash, passwordHashMethod, languageCode, appAssembly, connectedFunc, ConnectOperation.ConnectAndCreateAccount);
        }

        public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc)
        {
            return TryConnectInner(domain, hostName, portNumber, accountName, passwordHash, passwordHashMethod, languageCode, appAssembly, connectedFunc, ConnectOperation.ConnectAndConnectToAccount);
        }

        private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
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
                            tcs.TrySetResult(true);
                        }
                        break;

                    case XmppState.Registering:
                        registering = true;
                        break;

                    case XmppState.Connected:
                        tcs.TrySetResult(true);
                        break;

                    case XmppState.Offline:
                    case XmppState.Error:
                        tcs.TrySetResult(false);
                        break;
                }

                return Task.CompletedTask;
            }

            try
            {
                using (XmppClient client = new XmppClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod, languageCode, appAssembly))
                {
                    if (operation == ConnectOperation.ConnectAndCreateAccount)
                    {
                        if (this.tagProfile.TryGetKeys(domain, out string Key, out string Secret))
                            client.AllowRegistration(Key, Secret);
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
                        tcs.TrySetResult(false);
                    }

                    using (Timer _ = new Timer(TimerCallback, null, (int)ConnectTimeout.TotalMilliseconds, Timeout.Infinite))
                    {
                        succeeded = await tcs.Task;
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

        //public XmppClient CreateClient(string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly)
        //{
        //    return new XmppClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod, languageCode, appAssembly, sniffer);
        //}

        #region Legal Identity

        public event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

        private void OnLegalIdentityChanged(LegalIdentityChangedEventArgs e)
        {
            LegalIdentityChanged?.Invoke(this, e);
        }

        public async Task AddLegalIdentity(List<Property> properties, params LegalIdentityAttachment[] attachments)
        {
            AssertContractsIsAvailable();
            AssertFileUploadIsAvailable();

            LegalIdentity Identity = await contractsClient.ApplyAsync(properties.ToArray());

            foreach (var a in attachments)
            {
                HttpFileUploadEventArgs e2 = await fileUploadClient.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
                if (!e2.Ok)
                {
                    throw new Exception(e2.ErrorText);
                }

                await e2.PUT(a.Data, a.ContentType, (int)FileUploadTimeout.TotalMilliseconds);

                byte[] Signature = await contractsClient.SignAsync(a.Data);

                Identity = await contractsClient.AddLegalIdAttachmentAsync(Identity.Id, e2.GetUrl, Signature);
            }

            this.tagProfile.SetLegalIdentity(Identity);
        }

        public Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetLegalIdentityAsync(legalIdentityId);
        }

        public Task PetitionIdentityAsync(string legalId, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionIdentityAsync(legalId, petitionId, purpose);
        }

        public Task SendPetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionIdentityResponseAsync(legalId, petitionId, requestorFullJid, response);
        }

        public Task SendPetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionContractResponseAsync(contractId, petitionId, requestorFullJid, response);
        }

        public Task SendPetitionSignatureResponseAsync(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionSignatureResponseAsync(legalId, content, signature, petitionId, requestorFullJid, response);
        }

        public Task PetitionPeerReviewIDAsync(string legalId, LegalIdentity identity, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionPeerReviewIDAsync(legalId, identity, petitionId, purpose);
        }

        public Task<byte[]> SignAsync(byte[] data)
        {
            AssertContractsIsAvailable();
            return contractsClient.SignAsync(data);
        }

        public Task<LegalIdentity[]> GetLegalIdentitiesAsync()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetLegalIdentitiesAsync();
        }

        public Task<LegalIdentity> ObsoleteLegalIdentityAsync(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
        }

        public Task<LegalIdentity> CompromisedLegalIdentityAsync(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.CompromisedLegalIdentityAsync(legalIdentityId);
        }

        #endregion

        #region Contracts

        public Task PetitionContractAsync(string contractId, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionContractAsync(contractId, petitionId, purpose);
        }

        public Task<Contract> GetContractAsync(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetContractAsync(contractId);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetAttachmentAsync(url);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url, TimeSpan timeout)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetAttachmentAsync(url, (int)timeout.TotalMilliseconds);
        }

        public Task<Contract> CreateContractAsync(
            string templateId,
            Part[] parts,
            Parameter[] parameters,
            ContractVisibility visibility,
            ContractParts partsMode,
            Duration duration,
            Duration archiveRequired,
            Duration archiveOptional,
            DateTime? signAfter,
            DateTime? signBefore,
            bool canActAsTemplate)
        {
            AssertContractsIsAvailable();
            return contractsClient.CreateContractAsync(templateId, parts, parameters, visibility, partsMode, duration, archiveRequired, archiveOptional, signAfter, signBefore, canActAsTemplate);
        }

        public Task<Contract> DeleteContractAsync(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.DeleteContractAsync(contractId);
        }

        public Task<string[]> GetCreatedContractsAsync()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetCreatedContractsAsync();
        }

        public Task<string[]> GetSignedContractsAsync()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetSignedContractsAsync();
        }

        public Task<Contract> SignContractAsync(Contract contract, string role, bool transferable)
        {
            AssertContractsIsAvailable();
            return contractsClient.SignContractAsync(contract, role, transferable);
        }

        public Task<Contract> ObsoleteContractAsync(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.ObsoleteContractAsync(contractId);
        }

        #endregion

        private void AssertContractsIsAvailable()
        {
            if (contractsClient == null || string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
            {
                throw new XmppFeatureNotSupportedException("Contracts is not supported");
            }
        }

        private void AssertFileUploadIsAvailable()
        {
            if (fileUploadClient == null || !this.tagProfile.FileUploadIsSupported)
            {
                throw new XmppFeatureNotSupportedException("FileUpload is not supported");
            }
        }

        #region Configuration

        public bool FileUploadIsSupported =>
            tagProfile.FileUploadIsSupported &&
            !(fileUploadClient is null) && fileUploadClient.HasSupport;

        #endregion

        public async Task<bool> CheckServices()
        {
            if (xmppClient == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(this.tagProfile.LegalJid) ||
                string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid) ||
                !this.tagProfile.HttpFileUploadMaxSize.HasValue)
            {
                TaskCompletionSource<bool> stateChanged = new TaskCompletionSource<bool>();
                Task OnStateChanged(object sender, XmppState newState)
                {
                    if (newState == XmppState.Connected || newState == XmppState.Error)
                        stateChanged.TrySetResult(true);

                    return Task.CompletedTask;
                }

                xmppClient.OnStateChanged += OnStateChanged;

                if (xmppClient.State == XmppState.Connected || xmppClient.State == XmppState.Error)
                    stateChanged.TrySetResult(true);

                Task _ = Task.Delay(ConnectTimeout).ContinueWith((T) => stateChanged.TrySetResult(false));

                bool succeeded = await stateChanged.Task;

                xmppClient.OnStateChanged -= OnStateChanged;

                if (succeeded)
                    return await DiscoverServices();

                return false;
            }

            return true;
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

        private Task ContractsClient_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            // TODO: where to listen to/fire event and switch page?

            //if (!e.Response || e.RequestedContract is null)
            //	Device.BeginInvokeOnMainThread(() => this.messageService.DisplayAlert("Message", "Petition to view contract was denied.", AppResources.Ok));
            //else
            //	App.ShowPage(new ViewContractPage(App.Instance.MainPage, e.RequestedContract, false), false);

            return Task.CompletedTask;
        }

        private async Task ContractsClient_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            try
            {
                Contract Contract = await contractsClient.GetContractAsync(e.RequestedContractId);

                if (Contract.State == ContractState.Deleted ||
                    Contract.State == ContractState.Rejected)
                {
                    await contractsClient.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
                }
                else
                {
                    // TODO: where to listen to/fire event and switch page?
                    //App.ShowPage(new PetitionContractPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorFullJid, Contract, e.PetitionId, e.Purpose), false);
                }
            }
            catch (Exception)
            {
                await contractsClient.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
            }
        }

        private Task ContractsClient_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            // TODO: where to listen to/fire event and switch page?

            //         if (!e.Response || e.RequestedIdentity is null)
            //	Device.BeginInvokeOnMainThread(() => this.messageService.DisplayAlert(AppResources.Message, "Petition to view legal identity was denied.", AppResources.Ok));
            //else
            //	App.ShowPage(new Views.IdentityPage(App.Instance.MainPage, e.RequestedIdentity), false);

            return Task.CompletedTask;
        }

        private async Task ContractsClient_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            try
            {
                LegalIdentity identity;

                if (e.RequestedIdentityId == this.tagProfile.LegalIdentity.Id)
                    identity = this.tagProfile.LegalIdentity;
                else
                    identity = await contractsClient.GetLegalIdentityAsync(e.RequestedIdentityId);

                if (identity.State == IdentityState.Compromised ||
                    identity.State == IdentityState.Rejected)
                {
                    await contractsClient.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
                }
                else
                {
                    // TODO: where to listen to/fire event and switch page?
                    //App.ShowPage(new PetitionIdentityPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose), false);
                }
            }
            catch (Exception)
            {
                await contractsClient.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
            }
        }

        private async Task ContractsClient_PetitionedPeerReviewIDResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                if (!e.Response)
                {
                    Device.BeginInvokeOnMainThread(async () => await this.messageService.DisplayAlert(AppResources.PeerReviewRejected,
                        "A peer you requested to review your application, has rejected to approve it.", AppResources.Ok));
                }
                else
                {
                    StringBuilder xml = new StringBuilder();
                    tagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
                    byte[] Data = Encoding.UTF8.GetBytes(xml.ToString());

                    bool? Result = contractsClient.ValidateSignature(e.RequestedIdentity, Data, e.Signature);
                    if (!Result.HasValue || !Result.Value)
                    {
                        Device.BeginInvokeOnMainThread(async () => await this.messageService.DisplayAlert(AppResources.PeerReviewRejected,
                            "A peer review you requested has been rejected, due to a signature error.", AppResources.Ok));
                    }
                    else
                    {
                        await contractsClient.AddPeerReviewIDAttachment(tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature);
                        Device.BeginInvokeOnMainThread(async () => await this.messageService.DisplayAlert(AppResources.PeerReviewAccepted,
                            "A peer review you requested has been accepted.", AppResources.Ok));
                    }
                }
            }
            catch (Exception ex)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }
        }

        private Task ContractsClient_PetitionForPeerReviewIDReceived(object sender, SignaturePetitionEventArgs e)
        {
            // TODO: where to listen to event and switch page?
            //App.ShowPage(new IdentityPage(App.CurrentPage, e.RequestorIdentity, e), false);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionedSignatureResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            // Reject all signature requests by default
            return contractsClient.PetitionSignatureResponseAsync(e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false);
        }

        private Task ContractsClient_IdentityUpdated(object sender, LegalIdentityEventArgs e)
        {
            if (this.tagProfile.LegalIdentity is null ||
                this.tagProfile.LegalIdentity.Id == e.Identity.Id ||
                this.tagProfile.LegalIdentity.Created < e.Identity.Created)
            {
                this.tagProfile.SetLegalIdentity(e.Identity);
                OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
            }

            return Task.CompletedTask;
        }

        private void ReconnectTimer_Tick(object _)
        {
            if (!(xmppClient is null) && (xmppClient.State == XmppState.Error || xmppClient.State == XmppState.Offline))
                xmppClient.Reconnect();
        }

        private class InternalSink : EventSink
        {
            public InternalSink()
                : base("InternalEventSink")
            {
            }

            public override Task Queue(Event _)
            {
                return Task.CompletedTask;
            }
        }
    }
}