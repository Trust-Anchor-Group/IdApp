using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Waher.Events;
using Waher.IoTGateway.Setup;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Runtime.Settings;
using Waher.Runtime.Temporary;
using Waher.Security;
using XamarinApp.Views.Contracts;

namespace XamarinApp.Services
{
    internal sealed class TagService : ITagService
    {
        private static readonly RandomNumberGenerator rnd = RandomNumberGenerator.Create();
        private const string ConfigKey = "config";
        private Timer minuteTimer = null;
        private ContractsClient contracts = null;
        private HttpFileUploadClient fileUpload = null;
        private XmppConfiguration configuration = null;
        private string domainName = null;
        private string accountName = null;
        private string passwordHash = null;
        private string passwordHashMethod = null;
        private bool xmppSettingsOk = false;
        private bool initHasRun = false;

		public TagService()
        {
        }

        public void Dispose()
        {
            minuteTimer?.Dispose();
            minuteTimer = null;

            contracts?.Dispose();
            contracts = null;

            fileUpload?.Dispose();
            fileUpload = null;

            if (Xmpp != null)
            {
                using (ManualResetEvent OfflineSent = new ManualResetEvent(false))
                {
                    Xmpp.SetPresence(Availability.Offline, (sender, e) => OfflineSent.Set());
                    OfflineSent.WaitOne(1000);
                }

                Xmpp.Dispose();
                Xmpp = null;
            }

		}

		public async Task Init()
        {
            if (!initHasRun)
            {
                initHasRun = true;

                Log.Register(new InternalSink());

                try
                {
                    Types.Initialize(
                        typeof(App).Assembly,
                        typeof(Database).Assembly,
                        typeof(FilesProvider).Assembly,
                        typeof(ObjectSerializer).Assembly,
                        typeof(XmppClient).Assembly,
                        typeof(ContractsClient).Assembly,
                        //typeof(ThingReference).Assembly,
                        typeof(RuntimeSettings).Assembly,
                        typeof(Language).Assembly,
                        typeof(DnsResolver).Assembly,
                        //typeof(SensorClient).Assembly,
                        //typeof(ControlClient).Assembly,
                        //typeof(ConcentratorClient).Assembly,
                        typeof(XmppServerlessMessaging).Assembly,
                        typeof(ProvisioningClient).Assembly);
                    //typeof(EllipticCurve).Assembly);
                }
                catch (Exception e)
                {
                    await App.Instance.MainPage.DisplayAlert("Types", e.ToString(), "OK");
                    return;
                }

				try
                {
                    string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string DataFolder = Path.Combine(AppDataFolder, "Data");

                    FilesProvider Provider = await FilesProvider.CreateAsync(DataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, 10000, this.GetCustomKey);

                    try
                    {
                        await Provider.GetObjectSerializer(typeof(Attachment));
                    }
                    catch (Exception e)
                    {
                        await App.Instance.MainPage.DisplayAlert("Serializer/Attachment", e.ToString(), "OK");
                        return;
                    }

                    try
                    {
                        await Provider.GetObjectSerializer(typeof(Property));
                    }
                    catch (Exception e)
                    {
						await App.Instance.MainPage.DisplayAlert("Serializer/Property", e.ToString(), "OK");
                        return;
                    }

                    try
                    {
                        await Provider.GetObjectSerializer(typeof(LegalIdentity));
                    }
                    catch (Exception e)
                    {
						await App.Instance.MainPage.DisplayAlert("Serializer/LI", e.ToString(), "OK");
                        return;
                    }

                    try
                    {
                        await Provider.GetObjectSerializer(typeof(XmppConfiguration));
                    }
                    catch (Exception e)
                    {
						await App.Instance.MainPage.DisplayAlert("Serializer/Xmpp", e.ToString(), "OK");
                        return;
                    }

					await Provider.RepairIfInproperShutdown(string.Empty);

                    Database.Register(Provider);
                }
                catch (Exception e)
                {
                    await App.Instance.MainPage.DisplayAlert("Provider", e.ToString(), "OK");
                    return;
                }

                try
                {
                    //configuration = _sessionService.RestoreState<XmppConfiguration>(ConfigKey);
                    configuration = await Database.FindFirstDeleteRest<XmppConfiguration>();
                    if (configuration is null)
                    {
                        configuration = new XmppConfiguration();

						//_sessionService.SaveState(ConfigKey, configuration);
                        await Database.Insert(configuration);
                    }
                }
                catch (Exception e)
                {
					await App.Instance.MainPage.DisplayAlert("Configuration", e.ToString(), "OK");
                }
			}
		}

        private void GetCustomKey(string FileName, out byte[] Key, out byte[] IV)
        {
            string s;
            int i;

            try
            {
                s = SecureStorage.GetAsync(FileName).Result;
            }
            catch (TypeInitializationException)
            {
                // No secure storage available.

                Key = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(FileName + ".Key"));
                IV = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(FileName + ".IV"));
                Array.Resize<byte>(ref IV, 16);

                return;
            }

            if (!string.IsNullOrEmpty(s) && (i = s.IndexOf(',')) > 0)
            {
                Key = Hashes.StringToBinary(s.Substring(0, i));
                IV = Hashes.StringToBinary(s.Substring(i + 1));
            }
            else
            {
                Key = new byte[32];
                IV = new byte[16];

                lock (rnd)
                {
                    rnd.GetBytes(Key);
                    rnd.GetBytes(IV);
                }

                s = Hashes.BinaryToString(Key) + "," + Hashes.BinaryToString(IV);
                SecureStorage.SetAsync(FileName, s).Wait();
            }
        }

        public string CreateRandomPassword()
        {
            return Hashes.BinaryToString(GetBytes(16));
        }

        public async Task<(string hostName, int port)> GetXmppHostnameAndPort(string DomainName)
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

		private byte[] GetBytes(int NrBytes)
        {
            byte[] Result = new byte[NrBytes];

            lock (rnd)
            {
                rnd.GetBytes(Result);
            }

            return Result;
        }

        public void UpdateConfiguration()
        {
            Database.Update(this.configuration);
            //_sessionService.SaveState(ConfigKey, this.configuration);
        }

        public bool FileUploadIsSupported =>
            !string.IsNullOrEmpty(configuration.HttpFileUploadJid) &&
            configuration.HttpFileUploadMaxSize.HasValue &&
            !(fileUpload is null) &&
            fileUpload.HasSupport;

		public bool LegalIdentityIsValid => configuration.LegalIdentity is null ||
                                            configuration.LegalIdentity.State == IdentityState.Compromised ||
                                            configuration.LegalIdentity.State == IdentityState.Obsoleted ||
                                            configuration.LegalIdentity.State == IdentityState.Rejected;

        public bool PinIsValid => !configuration.UsePin || !string.IsNullOrEmpty(configuration.PinHash);

		public async Task UpdateXmpp()
		{
			if (Xmpp is null ||
				domainName != configuration.Domain ||
				accountName != configuration.Account ||
				passwordHash != configuration.PasswordHash ||
				passwordHashMethod != configuration.PasswordHashMethod)
			{
				Dispose();

				domainName = configuration.Domain;
				accountName = configuration.Account;
				passwordHash = configuration.PasswordHash;
				passwordHashMethod = configuration.PasswordHashMethod;

				(string HostName, int PortNumber) = await GetXmppHostnameAndPort(domainName);

				Xmpp = new XmppClient(HostName, PortNumber, accountName, passwordHash, passwordHashMethod, "en",
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

				Xmpp.OnStateChanged += (sender2, NewState) =>
				{
					switch (NewState)
					{
						case XmppState.Connected:
							xmppSettingsOk = true;

							minuteTimer?.Dispose();
							minuteTimer = null;

							minuteTimer = new Timer(MinuteTick, null, 60000, 60000);


							if (string.IsNullOrEmpty(configuration.LegalJid) ||
								string.IsNullOrEmpty(configuration.RegistryJid) ||
								string.IsNullOrEmpty(configuration.ProvisioningJid) ||
								string.IsNullOrEmpty(configuration.HttpFileUploadJid))
							{
								Task _ = FindServices(Xmpp);
							}
							break;

						case XmppState.Error:
							if (xmppSettingsOk)
							{
								xmppSettingsOk = false;
								Xmpp?.Reconnect();
							}
							break;
					}

					if (App.Instance.MainPage is IConnectionStateChanged ConnectionStateChanged)
						Device.BeginInvokeOnMainThread(() => ConnectionStateChanged.ConnectionStateChanged(NewState));

					return Task.CompletedTask;
				};

				Xmpp.Connect(domainName);
				minuteTimer = new Timer(MinuteTick, null, 60000, 60000);

				await AddLegalService(configuration.LegalJid);
				AddFileUploadService(configuration.HttpFileUploadJid, configuration.HttpFileUploadMaxSize);
			}
		}

		public async Task<bool> CheckServices()
		{
			if (string.IsNullOrEmpty(configuration.LegalJid) ||
				string.IsNullOrEmpty(configuration.HttpFileUploadJid) ||
				!configuration.HttpFileUploadMaxSize.HasValue)
			{
				ManualResetEvent Done = new ManualResetEvent(false);
				Task h(object sender, XmppState NewState)
				{
					if (NewState == XmppState.Connected || NewState == XmppState.Error)
						Done.Set();

					return Task.CompletedTask;
				}

				try
				{
					Xmpp.OnStateChanged += h;

					if (Xmpp.State == XmppState.Connected || Xmpp.State == XmppState.Error)
						Done.Set();

					if (Done.WaitOne(10000))
						return await FindServices(Xmpp);
					else
						return false;
				}
				finally
				{
					Xmpp.OnStateChanged -= h;
					Done.Dispose();
				}
			}
			else
				return true;
		}

		public async Task<bool> FindServices(XmppClient Client)
		{
			ServiceItemsDiscoveryEventArgs e2 = await Client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);
			bool Changed = false;

			foreach (Item Item in e2.Items)
			{
				ServiceDiscoveryEventArgs e3 = await Client.ServiceDiscoveryAsync(null, Item.JID, Item.Node);

				if (e3.HasFeature(ContractsClient.NamespaceLegalIdentities) &&
					e3.HasFeature(ContractsClient.NamespaceLegalIdentities))
				{
					if (configuration.LegalJid != Item.JID)
					{
						configuration.LegalJid = Item.JID;
						Changed = true;
					}
				}

				if (e3.HasFeature(ThingRegistryClient.NamespaceDiscovery))
				{
					if (configuration.RegistryJid != Item.JID)
					{
						configuration.RegistryJid = Item.JID;
						Changed = true;
					}
				}

				if (e3.HasFeature(ProvisioningClient.NamespaceProvisioningDevice) &&
					e3.HasFeature(ProvisioningClient.NamespaceProvisioningOwner) &&
					e3.HasFeature(ProvisioningClient.NamespaceProvisioningToken))
				{
					if (configuration.ProvisioningJid != Item.JID)
					{
						configuration.ProvisioningJid = Item.JID;
						Changed = true;
					}
				}

				if (e3.HasFeature(HttpFileUploadClient.Namespace))
				{
					if (configuration.HttpFileUploadJid != Item.JID)
					{
						configuration.HttpFileUploadJid = Item.JID;
						Changed = true;
					}

					long? MaxSize = HttpFileUploadClient.FindMaxFileSize(Client, e3);

					if (configuration.HttpFileUploadMaxSize != MaxSize)
					{
						configuration.HttpFileUploadMaxSize = MaxSize;
						Changed = true;
					}
				}
			}

			if (Changed)
                UpdateConfiguration();

			bool Result = true;

			if (!string.IsNullOrEmpty(configuration.LegalJid))
				await AddLegalService(configuration.LegalJid);
			else
				Result = false;

			if (!string.IsNullOrEmpty(configuration.HttpFileUploadJid) && configuration.HttpFileUploadMaxSize.HasValue)
				AddFileUploadService(configuration.HttpFileUploadJid, configuration.HttpFileUploadMaxSize);
			else
				Result = false;

			return Result;
		}

        public bool IsOnline
        {
            get
            {
                return !(Xmpp is null) && Xmpp.State == XmppState.Connected;
            }
        }

        public XmppClient Xmpp { get; private set; } = null;

		public ContractsClient Contracts { get; }

		public event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

        private void OnLegalIdentityChanged(LegalIdentityChangedEventArgs e)
        {
            LegalIdentityChanged?.Invoke(this, e);
        }

        public XmppConfiguration Configuration => configuration;

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

        public Task<LegalIdentity[]> GetLegalIdentitiesAsync()
        {
            return contracts.GetLegalIdentitiesAsync();
        }

        public bool HasLegalIdentityAttachments => configuration.LegalIdentity.Attachments != null;

        public Attachment[] GetLegalIdentityAttachments()
        {
            return configuration.LegalIdentity.Attachments;
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetAttachmentAsync(string url)
        {
            return contracts.GetAttachmentAsync(url);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetAttachmentAsync(string url, TimeSpan timeout)
        {
            return contracts.GetAttachmentAsync(url, (int)timeout.TotalMilliseconds);
        }

		internal async Task AddLegalService(string JID)
		{
			if (!string.IsNullOrEmpty(JID) && !(Xmpp is null))
			{
				contracts = await ContractsClient.Create(Xmpp, JID);
				contracts.IdentityUpdated += Contracts_IdentityUpdated;
				contracts.PetitionedIdentityReceived += Contracts_PetitionedIdentityReceived;
				contracts.PetitionedIdentityResponseReceived += Contracts_PetitionedIdentityResponseReceived;
				contracts.PetitionedContractReceived += Contracts_PetitionedContractReceived;
				contracts.PetitionedContractResponseReceived += Contracts_PetitionedContractResponseReceived;
			}
		}

		internal void AddFileUploadService(string JID, long? MaxFileSize)
		{
			if (!string.IsNullOrEmpty(JID) && MaxFileSize.HasValue && !(Xmpp is null))
				fileUpload = new HttpFileUploadClient(Xmpp, JID, MaxFileSize);
		}

		private Task Contracts_PetitionedContractResponseReceived(object Sender, ContractPetitionResponseEventArgs e)
		{
			if (!e.Response || e.RequestedContract is null)
				Device.BeginInvokeOnMainThread(() => App.Instance.MainPage.DisplayAlert("Message", "Petition to view contract was denied.", "OK"));
			else
				App.ShowPage(new ViewContractPage(App.Instance.MainPage, e.RequestedContract, false), false);

			return Task.CompletedTask;
		}

		private async Task Contracts_PetitionedContractReceived(object Sender, ContractPetitionEventArgs e)
		{
			try
			{
				Contract Contract = await contracts.GetContractAsync(e.RequestedContractId);

				if (Contract.State == ContractState.Deleted ||
					Contract.State == ContractState.Rejected)
				{
					await contracts.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorBareJid, false);
				}
				else
				{
					App.ShowPage(new PetitionContractPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorBareJid,
						Contract, e.PetitionId, e.Purpose), false);
				}
			}
			catch (Exception)
			{
				await contracts.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorBareJid, false);
			}
		}

		private Task Contracts_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			if (!e.Response || e.RequestedIdentity is null)
				Device.BeginInvokeOnMainThread(() => App.Instance.MainPage.DisplayAlert("Message", "Petition to view legal identity was denied.", "OK"));
			else
				App.ShowPage(new MainMenu.IdentityPage(App.Instance.MainPage, e.RequestedIdentity), false);

			return Task.CompletedTask;
		}

		private async Task Contracts_PetitionedIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity;

				if (e.RequestedIdentityId == configuration.LegalIdentity.Id)
					Identity = configuration.LegalIdentity;
				else
					Identity = await contracts.GetLegalIdentityAsync(e.RequestedIdentityId);

				if (Identity.State == IdentityState.Compromised ||
					Identity.State == IdentityState.Rejected)
				{
					await contracts.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorBareJid, false);
				}
				else
				{
					App.ShowPage(new PetitionIdentityPage(App.Instance.MainPage, e.RequestorIdentity, e.RequestorBareJid,
						e.RequestedIdentityId, e.PetitionId, e.Purpose), false);
				}
			}
			catch (Exception)
			{
				await contracts.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorBareJid, false);
			}
		}

		private Task Contracts_IdentityUpdated(object Sender, LegalIdentityEventArgs e)
		{
			if (configuration is null)
				return Task.CompletedTask;

			if (configuration.LegalIdentity is null ||
				configuration.LegalIdentity.Id == e.Identity.Id ||
				configuration.LegalIdentity.Created < e.Identity.Created)
			{
				configuration.LegalIdentity = e.Identity;
                UpdateConfiguration();

				OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
				if (App.Instance.MainPage is ILegalIdentityChanged LegalIdentityChanged)
					Device.BeginInvokeOnMainThread(() => LegalIdentityChanged.LegalIdentityChanged(e.Identity));
			}

            return Task.CompletedTask;
        }

		private void MinuteTick(object _)
		{
			if (!(Xmpp is null) && (Xmpp.State == XmppState.Error || Xmpp.State == XmppState.Offline))
				Xmpp.Reconnect();
		}

		private class InternalSink : EventSink
        {
            public InternalSink()
                : base("InternalEventSink")
            {
            }

            public override Task Queue(Event Event)
            {
                return Task.CompletedTask;
            }
        }
    }
}