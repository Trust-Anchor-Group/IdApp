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
        private static readonly TimeSpan ConnectTimeoutInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan TimerInterval = TimeSpan.FromMinutes(1);

        private readonly TagProfile tagProfile;
        private Timer reconnectTimer = null;
        private XmppClient xmpp = null;
        private ContractsClient contracts = null;
        private HttpFileUploadClient fileUpload = null;
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
            this.tagProfile.Changed += TagProfile_Changed;
        }

        public void Dispose()
        {
            reconnectTimer?.Dispose();
            reconnectTimer = null;

            this.tagProfile.Changed -= TagProfile_Changed;

            DestroyContractsClient();
			DestroyFileUploadClient();
            DestroyXmppClient();
        }

        private async void TagProfile_Changed(object sender, EventArgs e)
        {
            if (this.tagProfile.Step >= RegistrationStep.Account && xmpp == null)
            {
                await CreateXmppClient();
            }
            else if (this.tagProfile.Step < RegistrationStep.Account && xmpp != null)
            {
                DestroyXmppClient();
            }
        }

        private async Task LazyLoadContracts()
        {
            if (xmpp != null && contracts == null)
                await CreateContractsClient();
        }

        private async Task CreateContractsClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid) && !(xmpp is null))
            {
                contracts = await ContractsClient.Create(xmpp, this.tagProfile.LegalJid);
                contracts.IdentityUpdated += Contracts_IdentityUpdated;
                contracts.PetitionForIdentityReceived += Contracts_PetitionForIdentityReceived;
                contracts.PetitionedIdentityResponseReceived += Contracts_PetitionedIdentityResponseReceived;
                contracts.PetitionForContractReceived += Contracts_PetitionForContractReceived;
                contracts.PetitionedContractResponseReceived += Contracts_PetitionedContractResponseReceived;
                contracts.PetitionForSignatureReceived += Contracts_PetitionForSignatureReceived;
                contracts.PetitionedSignatureResponseReceived += Contracts_PetitionedSignatureResponseReceived;
                contracts.PetitionForPeerReviewIDReceived += Contracts_PetitionForPeerReviewIDReceived;
                contracts.PetitionedPeerReviewIDResponseReceived += Contracts_PetitionedPeerReviewIDResponseReceived;
            }
		}

		private void DestroyContractsClient()
        {
			if (contracts != null)
            {
                contracts.IdentityUpdated -= Contracts_IdentityUpdated;
                contracts.PetitionForIdentityReceived -= Contracts_PetitionForIdentityReceived;
                contracts.PetitionedIdentityResponseReceived -= Contracts_PetitionedIdentityResponseReceived;
                contracts.PetitionForContractReceived -= Contracts_PetitionForContractReceived;
                contracts.PetitionedContractResponseReceived -= Contracts_PetitionedContractResponseReceived;
                contracts.PetitionForSignatureReceived -= Contracts_PetitionForSignatureReceived;
                contracts.PetitionedSignatureResponseReceived -= Contracts_PetitionedSignatureResponseReceived;
                contracts.PetitionForPeerReviewIDReceived -= Contracts_PetitionForPeerReviewIDReceived;
                contracts.PetitionedPeerReviewIDResponseReceived -= Contracts_PetitionedPeerReviewIDResponseReceived;
                contracts.Dispose();
            }
        }

		private void CreateFileUploadClient()
        {
            if (!string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid) && this.tagProfile.HttpFileUploadMaxSize.HasValue && !(xmpp is null))
            {
                fileUpload = new HttpFileUploadClient(xmpp, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize.Value);
            }
        }

		private void DestroyFileUploadClient()
        {
            fileUpload?.Dispose();
            fileUpload = null;
        }

        private async Task CreateXmppClient()
        {
            if (xmpp is null ||
                domainName != this.tagProfile.Domain ||
                accountName != this.tagProfile.Account ||
                passwordHash != this.tagProfile.PasswordHash ||
                passwordHashMethod != this.tagProfile.PasswordHashMethod)
            {
                DestroyXmppClient();

                domainName = this.tagProfile.Domain;
                accountName = this.tagProfile.Account;
                passwordHash = this.tagProfile.PasswordHash;
                passwordHashMethod = this.tagProfile.PasswordHashMethod;

                (string hostName, int portNumber) = await tagProfile.GetXmppHostnameAndPort(domainName);

                xmpp = CreateClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod, "en", typeof(App).Assembly);
                xmpp.TrustServer = false;
                xmpp.AllowCramMD5 = false;
                xmpp.AllowDigestMD5 = false;
                xmpp.AllowPlain = false;
                xmpp.AllowEncryption = true;
                xmpp.AllowScramSHA1 = true;
                xmpp.AllowScramSHA256 = true;

                xmpp.OnStateChanged += XmppClient_StateChanged;

                xmpp.Connect(domainName);

                reconnectTimer?.Dispose();
                reconnectTimer = new Timer(Timer_Tick, null, TimerInterval, TimerInterval);
            }
        }

        private void DestroyXmppClient()
        {
            reconnectTimer?.Dispose();
            if (xmpp != null)
            {
                xmpp.OnStateChanged -= XmppClient_StateChanged;
                xmpp.Dispose();
            }
            xmpp = null;
        }

        private async Task XmppClient_StateChanged(object sender, XmppState newState)
        {
            switch (newState)
            {
                case XmppState.Connected:
                    xmppSettingsOk = true;

                    reconnectTimer?.Dispose();
                    reconnectTimer = new Timer(Timer_Tick, null, TimerInterval, TimerInterval);

                    if (string.IsNullOrEmpty(this.tagProfile.LegalJid) ||
                        string.IsNullOrEmpty(this.tagProfile.RegistryJid) ||
                        string.IsNullOrEmpty(this.tagProfile.ProvisioningJid) ||
                        string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid))
                    {
                        await DiscoverServices();
                    }
                    break;

                case XmppState.Error:
                    if (xmppSettingsOk)
                    {
                        xmppSettingsOk = false;
                        xmpp?.Reconnect();
                    }
                    break;
            }

            OnConnectionStateChanged(new ConnectionStateChangedEventArgs(newState));
        }

        #region Lifecycle

        public override async Task Load()
        {
            if (!IsLoaded && !IsLoading)
            {
				BeginLoad();

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
					EndLoad(false);
                    return;
                }

                xmpp?.SetPresence(Availability.Online);

                EndLoad(true);
            }
		}

		public override async Task Unload()
        {
			BeginUnload();

            if (!(xmpp is null))
            {
                TaskCompletionSource<bool> offlineSent = new TaskCompletionSource<bool>();

                xmpp.SetPresence(Availability.Offline, (sender, e) => offlineSent.TrySetResult(true));
                Task _ = Task.Delay(1000).ContinueWith(__ => offlineSent.TrySetResult(false));

                await offlineSent.Task;
                xmpp.Dispose();
                xmpp = null;
            }

            await DatabaseModule.Flush();
            await Types.StopAllModules();
            Log.Unregister(this.internalSink);
            Log.Terminate();
            this.internalSink.Dispose();
            this.internalSink = null;

			EndUnload();
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

		public bool IsOnline
        {
            get
            {
                return !(xmpp is null) && xmpp.State == XmppState.Connected;
            }
        }

        public XmppState State => xmpp?.State ?? XmppState.Offline;
        public string Domain => xmpp?.Domain ?? string.Empty;
        public string Account => tagProfile.Account ?? string.Empty;
        public string Host => xmpp?.Host ?? string.Empty;
        public string BareJID => xmpp?.BareJID ?? string.Empty;

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

                    client.Connect(domainName);

                    void TimerCallback(object _)
                    {
                        timeout = true;
                        tcs.TrySetResult(false);
                    }

                    using (Timer _ = new Timer(TimerCallback, null, (int)ConnectTimeoutInterval.TotalMilliseconds, Timeout.Infinite))
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
                    errorMessage = string.Format(AppResources.CantConnectTo, domainName);
                else if (!streamOpened)
                    errorMessage = string.Format(AppResources.DomainIsNotAValidOperator, domainName);
                else if (!startingEncryption)
                    errorMessage = string.Format(AppResources.DomainDoesNotFollowEncryptionPolicy, domainName);
                else if (!authenticating)
                    errorMessage = string.Format(AppResources.UnableToAuthenticateWith, domain);
                else if (!registering)
                    errorMessage = string.Format(AppResources.OperatorDoesNotSupportRegisteringNewAccounts, domain);
                else if (operation == ConnectOperation.ConnectAndCreateAccount)
                    errorMessage = string.Format(AppResources.AccountNameAlreadyTaken, accountName);
                else if (operation == ConnectOperation.ConnectAndConnectToAccount)
                    errorMessage = string.Format(AppResources.InvalidUsernameOrPassword, accountName);
                else
                    errorMessage = string.Format(AppResources.UnableToConnectTo, domainName);
            }

            return (succeeded, errorMessage);
        }

		public XmppClient CreateClient(string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly)
        {
			return new XmppClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod, languageCode, appAssembly, sniffer);
		}

		#region Legal Identity

		public event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

		private void OnLegalIdentityChanged(LegalIdentityChangedEventArgs e)
		{
			LegalIdentityChanged?.Invoke(this, e);
		}

		public async Task AddLegalIdentity(List<Property> properties, params LegalIdentityAttachment[] attachments)
		{
			LegalIdentity Identity = await contracts.ApplyAsync(properties.ToArray());

			foreach (var a in attachments)
			{
				HttpFileUploadEventArgs e2 = await fileUpload.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
				if (!e2.Ok)
				{
					throw new Exception(e2.ErrorText);
				}

                // TODO: use constant for timeout value
				await e2.PUT(a.Data, a.ContentType, 30000);

				byte[] Signature = await contracts.SignAsync(a.Data);

				Identity = await contracts.AddLegalIdAttachmentAsync(Identity.Id, e2.GetUrl, Signature);
			}

            this.tagProfile.SetLegalIdentity(Identity);
		}

		public Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId)
		{
			return contracts.GetLegalIdentityAsync(legalIdentityId);
		}

		public Task PetitionIdentityAsync(string legalId, string petitionId, string purpose)
		{
			return contracts.PetitionIdentityAsync(legalId, petitionId, purpose);
		}

		public Task SendPetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response)
		{
			return contracts.PetitionIdentityResponseAsync(legalId, petitionId, requestorFullJid, response);
		}

		public Task SendPetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response)
		{
			return contracts.PetitionContractResponseAsync(contractId, petitionId, requestorFullJid, response);
		}

		public Task SendPetitionSignatureResponseAsync(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response)
        {
            return contracts.PetitionSignatureResponseAsync(legalId, content, signature, petitionId, requestorFullJid, response);
        }

		public Task PetitionPeerReviewIDAsync(string legalId, LegalIdentity identity, string petitionId, string purpose)
        {
            return contracts.PetitionPeerReviewIDAsync(legalId, identity, petitionId, purpose);
        }

		public async Task<byte[]> SignAsync(byte[] data)
        {
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.SignAsync(data);
            return new byte[0];
        }

		public async Task<LegalIdentity[]> GetLegalIdentitiesAsync()
        {
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.GetLegalIdentitiesAsync();
            return new LegalIdentity[0];
        }

		public async Task<LegalIdentity> ObsoleteLegalIdentityAsync(string legalIdentityId)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.ObsoleteLegalIdentityAsync(legalIdentityId);
            return null;
        }

        public async Task<LegalIdentity> CompromisedLegalIdentityAsync(string legalIdentityId)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.CompromisedLegalIdentityAsync(legalIdentityId);
            return null;
        }

        #endregion

        #region Contracts

        public async Task PetitionContractAsync(string contractId, string petitionId, string purpose)
        {
            await LazyLoadContracts();
            if (contracts != null)
                await contracts.PetitionContractAsync(contractId, petitionId, purpose);
        }

        public async Task<Contract> GetContractAsync(string contractId)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.GetContractAsync(contractId);
            return null;
        }

		public async Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.GetAttachmentAsync(url);
            return new KeyValuePair<string, TemporaryFile>(string.Empty, null);
		}

		public async Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url, TimeSpan timeout)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.GetAttachmentAsync(url, (int)timeout.TotalMilliseconds);
            return new KeyValuePair<string, TemporaryFile>(string.Empty, null);
		}

        public async Task<Contract> CreateContractAsync(
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
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.CreateContractAsync(templateId, parts, parameters, visibility, partsMode, duration, archiveRequired, archiveOptional, signAfter, signBefore, canActAsTemplate);
            return null;
        }

		public async Task<Contract> DeleteContractAsync(string contractId)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.DeleteContractAsync(contractId);
            return null;
        }

		public async Task<string[]> GetCreatedContractsAsync()
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await  contracts.GetCreatedContractsAsync();
            return new string[0];
		}

		public async Task<string[]> GetSignedContractsAsync()
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.GetSignedContractsAsync();
            return new string[0];
		}

		public async Task<Contract> SignContractAsync(Contract contract, string role, bool transferable)
        {
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.SignContractAsync(contract, role, transferable);
            return null;
        }

		public async Task<Contract> ObsoleteContractAsync(string contractId)
		{
            await LazyLoadContracts();
            if (contracts != null)
                return await contracts.ObsoleteContractAsync(contractId);
            return null;
        }

		#endregion

		#region Configuration

        public bool FileUploadIsSupported =>
            tagProfile.FileUploadIsSupported &&
            !(fileUpload is null) && fileUpload.HasSupport;

		#endregion

		public async Task<bool> CheckServices()
		{
            if (xmpp == null)
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

                xmpp.OnStateChanged += OnStateChanged;

                if (xmpp.State == XmppState.Connected || xmpp.State == XmppState.Error)
                    stateChanged.TrySetResult(true);

                Task _ = Task.Delay(ConnectTimeoutInterval).ContinueWith((T) => stateChanged.TrySetResult(false));

                bool succeeded = await stateChanged.Task;

                xmpp.OnStateChanged -= OnStateChanged;

                if (succeeded)
                    return await DiscoverServices();

                return false;
            }

            return true;
		}

		public async Task<bool> DiscoverServices(XmppClient client = null)
        {
            client = client ?? xmpp;
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

            bool succeeded = true;

            if (string.IsNullOrEmpty(this.tagProfile.LegalJid))
                succeeded = false;

            if (string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid) || !this.tagProfile.HttpFileUploadMaxSize.HasValue)
                succeeded = false;

            return succeeded;
        }

		private Task Contracts_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
		{
            // TODO: where to listen to/fire event and switch page?

            //if (!e.Response || e.RequestedContract is null)
            //	Device.BeginInvokeOnMainThread(() => this.messageService.DisplayAlert("Message", "Petition to view contract was denied.", AppResources.Ok));
            //else
            //	App.ShowPage(new ViewContractPage(App.Instance.MainPage, e.RequestedContract, false), false);

            return Task.CompletedTask;
		}

		private async Task Contracts_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
		{
			try
			{
				Contract Contract = await contracts.GetContractAsync(e.RequestedContractId);

				if (Contract.State == ContractState.Deleted ||
					Contract.State == ContractState.Rejected)
				{
					await contracts.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
				}
				else
				{
                    // TODO: where to listen to/fire event and switch page?
					//App.ShowPage(new PetitionContractPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorFullJid, Contract, e.PetitionId, e.Purpose), false);
				}
			}
			catch (Exception)
			{
				await contracts.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
			}
		}

		private Task Contracts_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
		{
            // TODO: where to listen to/fire event and switch page?

   //         if (!e.Response || e.RequestedIdentity is null)
			//	Device.BeginInvokeOnMainThread(() => this.messageService.DisplayAlert(AppResources.Message, "Petition to view legal identity was denied.", AppResources.Ok));
			//else
			//	App.ShowPage(new Views.IdentityPage(App.Instance.MainPage, e.RequestedIdentity), false);

			return Task.CompletedTask;
		}

		private async Task Contracts_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				LegalIdentity identity;

				if (e.RequestedIdentityId == this.tagProfile.LegalIdentity.Id)
					identity = this.tagProfile.LegalIdentity;
				else
					identity = await contracts.GetLegalIdentityAsync(e.RequestedIdentityId);

				if (identity.State == IdentityState.Compromised ||
					identity.State == IdentityState.Rejected)
				{
					await contracts.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
				}
				else
				{
                    // TODO: where to listen to/fire event and switch page?
					//App.ShowPage(new PetitionIdentityPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose), false);
                }
            }
			catch (Exception)
			{
				await contracts.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
			}
		}

		private async Task Contracts_PetitionedPeerReviewIDResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
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

					bool? Result = contracts.ValidateSignature(e.RequestedIdentity, Data, e.Signature);
					if (!Result.HasValue || !Result.Value)
					{
                        Device.BeginInvokeOnMainThread(async () => await this.messageService.DisplayAlert(AppResources.PeerReviewRejected,
							"A peer review you requested has been rejected, due to a signature error.", AppResources.Ok));
					}
					else
					{
						await contracts.AddPeerReviewIDAttachment(tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature);
                        Device.BeginInvokeOnMainThread(async () => await this.messageService.DisplayPromptAsync(AppResources.PeerReviewAccepted,
							"A peer review you requested has been accepted.", AppResources.Ok));
					}
				}
			}
			catch (Exception ex)
			{
				await this.messageService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
		}

		private Task Contracts_PetitionForPeerReviewIDReceived(object sender, SignaturePetitionEventArgs e)
		{
            // TODO: where to listen to event and switch page?
            //App.ShowPage(new IdentityPage(App.CurrentPage, e.RequestorIdentity, e), false);
            return Task.CompletedTask;
		}

		private Task Contracts_PetitionedSignatureResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
		{
			return Task.CompletedTask;
		}

		private Task Contracts_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
		{
			// Reject all signature requests by default
			return contracts.PetitionSignatureResponseAsync(e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false);
		}

		private Task Contracts_IdentityUpdated(object sender, LegalIdentityEventArgs e)
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

		private void Timer_Tick(object _)
		{
			if (!(xmpp is null) && (xmpp.State == XmppState.Error || xmpp.State == XmppState.Offline))
				xmpp.Reconnect();
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