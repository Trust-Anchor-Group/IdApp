using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Xamarin.Forms;
using Waher.IoTGateway.Setup;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Runtime.Temporary;
using XamarinApp.Views.Contracts;

namespace XamarinApp.Services
{
    internal sealed class TagService : ITagService
    {
        private static readonly TimeSpan TimerInterval = TimeSpan.FromMinutes(1);

        private Timer reconnectTimer = null;
        private XmppClient xmpp = null;
        private ContractsClient contracts = null;
        private HttpFileUploadClient fileUpload = null;
        private string domainName = null;
        private string accountName = null;
        private string passwordHash = null;
        private string passwordHashMethod = null;
        private bool xmppSettingsOk = false;
        private bool isLoaded = false;
        private bool isLoading = false;
        private readonly IMessageService messageService;

		public TagService(IMessageService messageService)
        {
            this.messageService = messageService;
        }

        public void Dispose()
        {
            reconnectTimer?.Dispose();
            reconnectTimer = null;

			if (contracts != null)
            {
				contracts.IdentityUpdated -= Contracts_IdentityUpdated;
                contracts.PetitionForIdentityReceived -= Contracts_PetitionForIdentityReceived;
                contracts.PetitionedIdentityResponseReceived -= Contracts_PetitionedIdentityResponseReceived;
                contracts.PetitionForContractReceived -= Contracts_PetitionForContractReceived;
                contracts.PetitionedContractResponseReceived -= Contracts_PetitionedContractResponseReceived;
                contracts.Dispose();
            }
			contracts = null;

            fileUpload?.Dispose();
            fileUpload = null;

            xmpp?.Dispose();
            xmpp = null;
        }

		#region Lifecycle

		public async Task Load()
        {
            if (!isLoaded && !isLoading)
            {
                isLoading = true;

                try
                {
                    Configuration = await Database.FindFirstDeleteRest<XmppConfiguration>();
                    if (Configuration is null)
                    {
                        Configuration = new XmppConfiguration();

                        await Database.Insert(Configuration);
                    }
                }
                catch (Exception e)
                {
                    isLoading = false;
					await this.messageService.DisplayAlert(AppResources.ErrorTitleText, e.ToString(), AppResources.OkButtonText);
                    return;
                }

				this.xmpp?.SetPresence(Availability.Online);

                isLoading = false;
                isLoaded = true;

				OnLoaded(new LoadedEventArgs(isLoaded));
            }
		}

		public async Task Unload()
        {
            if (!(xmpp is null))
            {
                TaskCompletionSource<bool> OfflineSent = new TaskCompletionSource<bool>();

                xmpp.SetPresence(Availability.Offline, (sender, e) => OfflineSent.TrySetResult(true));
                Task _ = Task.Delay(1000).ContinueWith((T) => OfflineSent.TrySetResult(false));

                await OfflineSent.Task;
                xmpp.Dispose();
                xmpp = null;
            }

            isLoaded = false;
			OnLoaded(new LoadedEventArgs(false));
        }

        private event EventHandler<LoadedEventArgs> loaded;

        public event EventHandler<LoadedEventArgs> Loaded
        {
            add
            {
                loaded += value;
                value(this, new LoadedEventArgs(isLoaded));
            }
            remove
            {
                loaded -= value;
            }
        }

        private void OnLoaded(LoadedEventArgs e)
        {
            loaded?.Invoke(this, e);
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
        public string Host => xmpp?.Host ?? string.Empty;
        public string BareJID => xmpp?.BareJID ?? string.Empty;
        public XmppConfiguration Configuration { get; private set; }

		#endregion

		#region Legal Identity

		public event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

		private void OnLegalIdentityChanged(LegalIdentityChangedEventArgs e)
		{
			LegalIdentityChanged?.Invoke(this, e);
		}

		public async Task AddLegalIdentity(List<Property> properties, params LegalIdentityAttachment[] attachments)
		{
			LegalIdentity Identity = await contracts.ApplyAsync(properties.ToArray());

			foreach (var P in attachments)
			{
				HttpFileUploadEventArgs e2 = await fileUpload.RequestUploadSlotAsync(Path.GetFileName(P.Filename), P.ContentType, P.ContentLength);
				if (!e2.Ok)
				{
					throw new Exception(e2.ErrorText);
				}

				await e2.PUT(P.Data, P.ContentType, 30000);

				byte[] Signature = await contracts.SignAsync(P.Data);

				Identity = await contracts.AddLegalIdAttachmentAsync(Identity.Id, e2.GetUrl, Signature);
			}

			Configuration.LegalIdentity = Identity;
			if (Configuration.Step == 2)
				Configuration.Step++;

			UpdateConfiguration();
		}

		public Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId)
		{
			return contracts.GetLegalIdentityAsync(legalIdentityId);
		}

		public Task PetitionIdentityAsync(string legalId, string petitionId, string purpose)
		{
			return contracts.PetitionIdentityAsync(legalId, petitionId, purpose);
		}

		public Task PetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response)
		{
			return contracts.PetitionIdentityResponseAsync(legalId, petitionId, requestorFullJid, response);
		}

		public Task PetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response)
		{
			return contracts.PetitionContractResponseAsync(contractId, petitionId, requestorFullJid, response);
		}

		public Task<LegalIdentity[]> GetLegalIdentitiesAsync()
		{
			return contracts.GetLegalIdentitiesAsync();
		}

		public bool HasLegalIdentityAttachments => this.Configuration.LegalIdentity.Attachments != null;

		public Attachment[] GetLegalIdentityAttachments()
		{
			return this.Configuration.LegalIdentity.Attachments;
		}

		public Task<LegalIdentity> ObsoleteLegalIdentityAsync(string legalIdentityId)
		{
			return contracts.ObsoleteLegalIdentityAsync(legalIdentityId);
		}

		public Task<LegalIdentity> CompromisedLegalIdentityAsync(string legalIdentityId)
		{
			return contracts.CompromisedLegalIdentityAsync(legalIdentityId);
		}

        public bool LegalIdentityIsValid => this.Configuration.LegalIdentity is null ||
                                            this.Configuration.LegalIdentity.State == IdentityState.Compromised ||
                                            this.Configuration.LegalIdentity.State == IdentityState.Obsoleted ||
                                            this.Configuration.LegalIdentity.State == IdentityState.Rejected;

		#endregion

		#region Contracts

		public Task PetitionContractAsync(string contractId, string petitionId, string purpose)
		{
			return contracts.PetitionContractAsync(contractId, petitionId, purpose);
		}

		public Task<Contract> GetContractAsync(string contractId)
		{
			return contracts.GetContractAsync(contractId);
		}

		public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url)
		{
			return contracts.GetAttachmentAsync(url);
		}

		public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url, TimeSpan timeout)
		{
			return contracts.GetAttachmentAsync(url, (int)timeout.TotalMilliseconds);
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
			return contracts.CreateContractAsync(templateId, parts, parameters, visibility, partsMode, duration, archiveRequired, archiveOptional, signAfter, signBefore, canActAsTemplate);
		}

		public Task<Contract> DeleteContractAsync(string contractId)
		{
			return contracts.DeleteContractAsync(contractId);
		}

		public Task<string[]> GetCreatedContractsAsync()
		{
			return contracts.GetCreatedContractsAsync();
		}

		public Task<string[]> GetSignedContractsAsync()
		{
			return contracts.GetSignedContractsAsync();
		}

		public Task<Contract> SignContractAsync(Contract contract, string role, bool transferable)
		{
			return contracts.SignContractAsync(contract, role, transferable);
		}

		public Task<Contract> ObsoleteContractAsync(string contractId)
		{
			return contracts.ObsoleteContractAsync(contractId);
		}

		#endregion

		#region Configuration

		public void SetPin(string pin, bool usePin)
        {
            Configuration.Pin = pin;
            Configuration.UsePin = usePin;
            UpdateConfiguration();
        }

        public void ResetPin()
        {
            Configuration.Pin = string.Empty;
            Configuration.UsePin = false;
            UpdateConfiguration();
        }

        public void SetAccount(string accountNameText, string clientPasswordHash, string clientPasswordHashMethod)
        {
            Configuration.Account = accountNameText;
            Configuration.PasswordHash = clientPasswordHash;
            Configuration.PasswordHashMethod = clientPasswordHashMethod;
            UpdateConfiguration();
        }

        public void SetDomain(string domainName, string legalJid)
        {
            Configuration.Domain = domainName;
            Configuration.LegalJid = legalJid;
            UpdateConfiguration();
        }

        public bool FileUploadIsSupported =>
            !string.IsNullOrEmpty(this.Configuration.HttpFileUploadJid) &&
            this.Configuration.HttpFileUploadMaxSize.HasValue &&
            !(fileUpload is null) &&
            fileUpload.HasSupport;

		#endregion

		public async Task<(string hostName, int port)> GetXmppHostnameAndPort(string DomainName = null)
        {
            DomainName = DomainName ?? Configuration.Domain;

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

		public void UpdateConfiguration()
        {
            Database.Update(this.Configuration);
        }

		public void DecrementConfigurationStep(int? stepToRevertTo = null)
        {
            if (stepToRevertTo.HasValue)
                Configuration.Step = stepToRevertTo.Value;
            else
                Configuration.Step--;
            UpdateConfiguration();
        }

		public void IncrementConfigurationStep()
        {
            Configuration.Step++;
            UpdateConfiguration();
        }

        public bool PinIsValid => !this.Configuration.UsePin || !string.IsNullOrEmpty(this.Configuration.PinHash);

		public async Task UpdateXmpp()
		{
			if (xmpp is null ||
				domainName != this.Configuration.Domain ||
				accountName != this.Configuration.Account ||
				passwordHash != this.Configuration.PasswordHash ||
				passwordHashMethod != this.Configuration.PasswordHashMethod)
			{
				Dispose();

				domainName = this.Configuration.Domain;
				accountName = this.Configuration.Account;
				passwordHash = this.Configuration.PasswordHash;
				passwordHashMethod = this.Configuration.PasswordHashMethod;

				(string HostName, int PortNumber) = await GetXmppHostnameAndPort(domainName);

				xmpp = new XmppClient(HostName, PortNumber, accountName, passwordHash, passwordHashMethod, "en",
					typeof(App).Assembly)
				{
					TrustServer = false,
					AllowCramMD5 = false,
					AllowDigestMD5 = false,
					AllowPlain = false,
					AllowEncryption = true,
					AllowScramSHA1 = true,
					AllowScramSHA256 = true
				};

				xmpp.OnStateChanged += async (sender2, NewState) =>
				{
					switch (NewState)
					{
						case XmppState.Connected:
							xmppSettingsOk = true;

							reconnectTimer?.Dispose();
							reconnectTimer = new Timer(Timer_Tick, null, TimerInterval, TimerInterval);

							if (string.IsNullOrEmpty(this.Configuration.LegalJid) ||
								string.IsNullOrEmpty(this.Configuration.RegistryJid) ||
								string.IsNullOrEmpty(this.Configuration.ProvisioningJid) ||
								string.IsNullOrEmpty(this.Configuration.HttpFileUploadJid))
							{
								await FindServices();
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

                    OnConnectionStateChanged(new ConnectionStateChangedEventArgs(NewState));
				};

				xmpp.Connect(domainName);

                reconnectTimer?.Dispose();
                reconnectTimer = new Timer(Timer_Tick, null, TimerInterval, TimerInterval);

				await AddLegalService(this.Configuration.LegalJid);
				AddFileUploadService(this.Configuration.HttpFileUploadJid, this.Configuration.HttpFileUploadMaxSize);
			}
		}

		public async Task<bool> CheckServices()
		{
			if (string.IsNullOrEmpty(this.Configuration.LegalJid) ||
				string.IsNullOrEmpty(this.Configuration.HttpFileUploadJid) ||
				!this.Configuration.HttpFileUploadMaxSize.HasValue)
			{
                TaskCompletionSource<bool> Done = new TaskCompletionSource<bool>();
				Task h(object sender, XmppState NewState)
				{
					if (NewState == XmppState.Connected || NewState == XmppState.Error)
						Done.TrySetResult(true);

					return Task.CompletedTask;
				}

				try
				{
					xmpp.OnStateChanged += h;

					if (xmpp.State == XmppState.Connected || xmpp.State == XmppState.Error)
						Done.TrySetResult(true);

                    Task _ = Task.Delay(10000).ContinueWith((T) => Done.TrySetResult(false));

                    if (await Done.Task)
                        return await FindServices();
                    else
                        return false;
                }
				finally
				{
					xmpp.OnStateChanged -= h;
				}
			}
    		return true;
		}

		public async Task<bool> FindServices(XmppClient Client = null)
        {
            Client = Client ?? xmpp;
			ServiceItemsDiscoveryEventArgs e2 = await Client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);
			bool Changed = false;

			foreach (Item Item in e2.Items)
			{
				ServiceDiscoveryEventArgs e3 = await Client.ServiceDiscoveryAsync(null, Item.JID, Item.Node);

				if (e3.HasFeature(ContractsClient.NamespaceLegalIdentities) &&
					e3.HasFeature(ContractsClient.NamespaceLegalIdentities))
				{
					if (this.Configuration.LegalJid != Item.JID)
					{
                        this.Configuration.LegalJid = Item.JID;
						Changed = true;
					}
				}

				if (e3.HasFeature(ThingRegistryClient.NamespaceDiscovery))
				{
					if (this.Configuration.RegistryJid != Item.JID)
					{
                        this.Configuration.RegistryJid = Item.JID;
						Changed = true;
					}
				}

				if (e3.HasFeature(ProvisioningClient.NamespaceProvisioningDevice) &&
					e3.HasFeature(ProvisioningClient.NamespaceProvisioningOwner) &&
					e3.HasFeature(ProvisioningClient.NamespaceProvisioningToken))
				{
					if (this.Configuration.ProvisioningJid != Item.JID)
					{
                        this.Configuration.ProvisioningJid = Item.JID;
						Changed = true;
					}
				}

				if (e3.HasFeature(HttpFileUploadClient.Namespace))
				{
					if (this.Configuration.HttpFileUploadJid != Item.JID)
					{
                        this.Configuration.HttpFileUploadJid = Item.JID;
						Changed = true;
					}

					long? MaxSize = HttpFileUploadClient.FindMaxFileSize(Client, e3);

					if (this.Configuration.HttpFileUploadMaxSize != MaxSize)
					{
                        this.Configuration.HttpFileUploadMaxSize = MaxSize;
						Changed = true;
					}
				}
			}

			if (Changed)
                UpdateConfiguration();

			bool Result = true;

			if (!string.IsNullOrEmpty(this.Configuration.LegalJid))
				await AddLegalService(this.Configuration.LegalJid);
			else
				Result = false;

			if (!string.IsNullOrEmpty(this.Configuration.HttpFileUploadJid) && this.Configuration.HttpFileUploadMaxSize.HasValue)
				AddFileUploadService(this.Configuration.HttpFileUploadJid, this.Configuration.HttpFileUploadMaxSize);
			else
				Result = false;

			return Result;
		}

		private async Task AddLegalService(string JID)
		{
			if (!string.IsNullOrEmpty(JID) && !(xmpp is null))
			{
				contracts = await ContractsClient.Create(xmpp, JID);
				contracts.IdentityUpdated += Contracts_IdentityUpdated;
				contracts.PetitionForIdentityReceived += Contracts_PetitionForIdentityReceived;
				contracts.PetitionedIdentityResponseReceived += Contracts_PetitionedIdentityResponseReceived;
				contracts.PetitionForContractReceived += Contracts_PetitionForContractReceived;
				contracts.PetitionedContractResponseReceived += Contracts_PetitionedContractResponseReceived;
			}
		}

		private void AddFileUploadService(string JID, long? MaxFileSize)
		{
			if (!string.IsNullOrEmpty(JID) && MaxFileSize.HasValue && !(xmpp is null))
				fileUpload = new HttpFileUploadClient(xmpp, JID, MaxFileSize);
		}

		private Task Contracts_PetitionedContractResponseReceived(object Sender, ContractPetitionResponseEventArgs e)
		{
			if (!e.Response || e.RequestedContract is null)
				Device.BeginInvokeOnMainThread(() => this.messageService.DisplayAlert("Message", "Petition to view contract was denied.", AppResources.OkButtonText));
			else
				App.ShowPage(new ViewContractPage(App.Instance.MainPage, e.RequestedContract, false), false);

			return Task.CompletedTask;
		}

		private async Task Contracts_PetitionForContractReceived(object Sender, ContractPetitionEventArgs e)
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
					App.ShowPage(new PetitionContractPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorFullJid,
						Contract, e.PetitionId, e.Purpose), false);
				}
			}
			catch (Exception)
			{
				await contracts.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
			}
		}

		private Task Contracts_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			if (!e.Response || e.RequestedIdentity is null)
				Device.BeginInvokeOnMainThread(() => this.messageService.DisplayAlert("Message", "Petition to view legal identity was denied.", AppResources.OkButtonText));
			else
				App.ShowPage(new Views.IdentityPage(App.Instance.MainPage, e.RequestedIdentity), false);

			return Task.CompletedTask;
		}

		private async Task Contracts_PetitionForIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity;

				if (e.RequestedIdentityId == this.Configuration.LegalIdentity.Id)
					Identity = this.Configuration.LegalIdentity;
				else
					Identity = await contracts.GetLegalIdentityAsync(e.RequestedIdentityId);

				if (Identity.State == IdentityState.Compromised ||
					Identity.State == IdentityState.Rejected)
				{
					await contracts.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
				}
				else
				{
					App.ShowPage(new PetitionIdentityPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorFullJid,
						e.RequestedIdentityId, e.PetitionId, e.Purpose), false);
				}
			}
			catch (Exception)
			{
				await contracts.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
			}
		}

		private Task Contracts_IdentityUpdated(object Sender, LegalIdentityEventArgs e)
		{
			if (this.Configuration is null)
				return Task.CompletedTask;

			if (this.Configuration.LegalIdentity is null ||
                this.Configuration.LegalIdentity.Id == e.Identity.Id ||
                this.Configuration.LegalIdentity.Created < e.Identity.Created)
			{
                this.Configuration.LegalIdentity = e.Identity;
                UpdateConfiguration();

				OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
				if (App.Instance.MainPage is ILegalIdentityChanged LegalIdentityChanged)
					Device.BeginInvokeOnMainThread(() => LegalIdentityChanged.LegalIdentityChanged(e.Identity));
			}

            return Task.CompletedTask;
        }

		private void Timer_Tick(object _)
		{
			if (!(xmpp is null) && (xmpp.State == XmppState.Error || xmpp.State == XmppState.Offline))
				xmpp.Reconnect();
		}
    }
}