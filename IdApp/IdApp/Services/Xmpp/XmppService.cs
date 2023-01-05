using EDaler;
using EDaler.Uris;
using IdApp.Extensions;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Registration.RegisterIdentity;
using IdApp.Popups.Xmpp.ReportOrBlock;
using IdApp.Popups.Xmpp.ReportType;
using IdApp.Popups.Xmpp.SubscribeTo;
using IdApp.Popups.Xmpp.SubscriptionRequest;
using IdApp.Services.Contracts;
using IdApp.Services.Messages;
using IdApp.Services.Navigation;
using IdApp.Services.Notification.Things;
using IdApp.Services.Notification.Xmpp;
using IdApp.Services.Push;
using IdApp.Services.Tag;
using IdApp.Services.UI.Photos;
using IdApp.Services.Wallet;
using NeuroFeatures;
using NeuroFeatures.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Abuse;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.HTTPX;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.PEP;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.Push;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Waher.Runtime.Settings;
using Waher.Runtime.Temporary;
using Waher.Security.JWT;
using Waher.Things;
using Waher.Things.SensorData;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Xmpp
{
	/// <summary>
	/// XMPP Service, maintaining XMPP connections and XMPP Extensions.
	///
	/// Note: By duplicating event handlers on the service, event handlers continue to work, even if app
	/// goes to sleep, and new clients are created when awoken again.
	/// </summary>
	[Singleton]
	internal sealed class XmppService : LoadableService, IXmppService
	{
		private readonly Assembly appAssembly;
		private Profiler startupProfiler;
		private ProfilerThread xmppThread;
		private XmppClient xmppClient;
		private ContractsClient contractsClient;
		private HttpFileUploadClient fileUploadClient;
		private MultiUserChatClient mucClient;
		private ThingRegistryClient thingRegistryClient;
		private ProvisioningClient provisioningClient;
		private ControlClient controlClient;
		private SensorClient sensorClient;
		private ConcentratorClient concentratorClient;
		private EDalerClient eDalerClient;
		private NeuroFeaturesClient neuroFeaturesClient;
		private PushNotificationClient pushNotificationClient;
		private AbuseClient abuseClient;
		private PepClient pepClient;
		private HttpxClient httpxClient;
		private Timer reconnectTimer;
		private string domainName;
		private string accountName;
		private string passwordHash;
		private string passwordHashMethod;
		private bool xmppConnected = false;
		private DateTime xmppLastStateChange = DateTime.MinValue;
		private readonly InMemorySniffer sniffer;
		private bool isCreatingClient;
		private XmppEventSink xmppEventSink;
		private string token = null;
		private DateTime tokenCreated = DateTime.MinValue;

		#region Creation / Destruction

		public XmppService(Assembly AppAssembly, Profiler StartupProfiler)
		{
			this.appAssembly = AppAssembly;
			this.sniffer = new(250);
			this.startupProfiler = StartupProfiler;
		}

		private async Task CreateXmppClient(bool CanCreateKeys, ProfilerThread Thread)
		{
			if (this.isCreatingClient)
				return;

			this.xmppThread = this.startupProfiler?.CreateThread("XMPP", ProfilerThreadType.StateMachine);
			this.xmppThread?.Start();
			this.xmppThread?.Idle();

			try
			{
				this.isCreatingClient = true;

				if (!this.XmppParametersCurrent() || this.XmppStale())
				{
					if (this.xmppClient is not null)
					{
						Thread?.NewState("Destroy");
						await this.DestroyXmppClient();
					}

					this.domainName = this.TagProfile.Domain;
					this.accountName = this.TagProfile.Account;
					this.passwordHash = this.TagProfile.PasswordHash;
					this.passwordHashMethod = this.TagProfile.PasswordHashMethod;

					string HostName;
					int PortNumber;
					bool IsIpAddress;

					if (this.TagProfile.DefaultXmppConnectivity)
					{
						HostName = this.domainName;
						PortNumber = XmppCredentials.DefaultPort;
						IsIpAddress = false;
					}
					else
					{
						Thread?.NewState("DNS");
						(HostName, PortNumber, IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(this.domainName);

						if (HostName == this.domainName && PortNumber == XmppCredentials.DefaultPort)
							await this.TagProfile.SetDomain(this.domainName, true, this.TagProfile.ApiKey, this.TagProfile.ApiSecret);
					}

					this.xmppLastStateChange = DateTime.Now;
					this.xmppConnected = false;

					Thread?.NewState("Client");
					if (string.IsNullOrEmpty(this.passwordHashMethod))
						this.xmppClient = new XmppClient(HostName, PortNumber, this.accountName, this.passwordHash, Constants.LanguageCodes.Default, this.appAssembly, this.sniffer);
					else
						this.xmppClient = new XmppClient(HostName, PortNumber, this.accountName, this.passwordHash, this.passwordHashMethod, Constants.LanguageCodes.Default, this.appAssembly, this.sniffer);

					this.xmppClient.TrustServer = !IsIpAddress;
					this.xmppClient.AllowCramMD5 = false;
					this.xmppClient.AllowDigestMD5 = false;
					this.xmppClient.AllowPlain = false;
					this.xmppClient.AllowEncryption = true;
					this.xmppClient.AllowScramSHA1 = true;
					this.xmppClient.AllowScramSHA256 = true;
					this.xmppClient.AllowQuickLogin = true;

					this.xmppClient.RequestRosterOnStartup = true;
					this.xmppClient.OnStateChanged += this.XmppClient_StateChanged;
					this.xmppClient.OnConnectionError += this.XmppClient_ConnectionError;
					this.xmppClient.OnError += this.XmppClient_Error;
					this.xmppClient.OnChatMessage += this.XmppClient_OnChatMessage;
					this.xmppClient.OnNormalMessage += this.XmppClient_OnNormalMessage;
					this.xmppClient.OnPresenceSubscribe += this.XmppClient_OnPresenceSubscribe;
					this.xmppClient.OnPresenceUnsubscribed += this.XmppClient_OnPresenceUnsubscribed;
					this.xmppClient.OnRosterItemAdded += this.XmppClient_OnRosterItemAdded;
					this.xmppClient.OnRosterItemUpdated += this.XmppClient_OnRosterItemUpdated;
					this.xmppClient.OnRosterItemRemoved += this.XmppClient_OnRosterItemRemoved;
					this.xmppClient.OnPresence += this.XmppClient_OnPresence;

					this.xmppClient.RegisterMessageHandler("Delivered", ContractsClient.NamespaceOnboarding, this.TransferIdDelivered, true);
					this.xmppClient.RegisterMessageHandler("clientMessage", ContractsClient.NamespaceLegalIdentities, this.ClientMessage, true);

					Thread?.NewState("Sink");
					this.xmppEventSink = new XmppEventSink("XMPP Event Sink", this.xmppClient, this.TagProfile.LogJid, false);

					// Add extensions before connecting

					this.abuseClient = new AbuseClient(this.xmppClient);

					if (!string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
					{
						Thread?.NewState("Legal");
						this.contractsClient = new ContractsClient(this.xmppClient, this.TagProfile.LegalJid);
						this.RegisterContractsEventHandlers();

						Thread?.NewState("Keys");
						if (!await this.contractsClient.LoadKeys(false))
						{
							if (!CanCreateKeys)
							{
								Log.Alert("Regeneration of keys not permitted at this time.",
									string.Empty, string.Empty, string.Empty, EventLevel.Major, string.Empty, string.Empty, Environment.StackTrace);

								throw new Exception("Regeneration of keys not permitted at this time.");
							}

							Thread?.NewState("Gen");
							await this.contractsClient.GenerateNewKeys();
						}
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.HttpFileUploadJid) && this.TagProfile.HttpFileUploadMaxSize.HasValue)
					{
						Thread?.NewState("Upload");
						this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, this.TagProfile.HttpFileUploadJid, this.TagProfile.HttpFileUploadMaxSize);
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.MucJid))
					{
						Thread?.NewState("MUC");
						this.mucClient = new MultiUserChatClient(this.xmppClient, this.TagProfile.MucJid);
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.RegistryJid))
					{
						Thread?.NewState("Reg");
						this.thingRegistryClient = new ThingRegistryClient(this.xmppClient, this.TagProfile.RegistryJid);
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.ProvisioningJid))
					{
						Thread?.NewState("Prov");
						this.provisioningClient = new ProvisioningClient(this.xmppClient, this.TagProfile.ProvisioningJid)
						{
							ManagePresenceSubscriptionRequests = false
						};

						this.provisioningClient.CanControlQuestion += this.ProvisioningClient_CanControlQuestion;
						this.provisioningClient.CanReadQuestion += this.ProvisioningClient_CanReadQuestion;
						this.provisioningClient.IsFriendQuestion += this.ProvisioningClient_IsFriendQuestion;
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.EDalerJid))
					{
						Thread?.NewState("eDaler");
						this.eDalerClient = new EDalerClient(this.xmppClient, this.contractsClient, this.TagProfile.EDalerJid);
						this.RegisterEDalerEventHandlers(this.eDalerClient);
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.NeuroFeaturesJid))
					{
						Thread?.NewState("Neuro-Features");
						this.neuroFeaturesClient = new NeuroFeaturesClient(this.xmppClient, this.contractsClient, this.TagProfile.NeuroFeaturesJid);
						this.RegisterNeuroFeatureEventHandlers(this.neuroFeaturesClient);
					}

					if (this.TagProfile.SupportsPushNotification.HasValue && this.TagProfile.SupportsPushNotification.Value)
					{
						Thread?.NewState("Push");
						this.pushNotificationClient = new PushNotificationClient(this.xmppClient);
					}

					Thread?.NewState("Sensor");
					this.sensorClient = new SensorClient(this.xmppClient);

					Thread?.NewState("Control");
					this.controlClient = new ControlClient(this.xmppClient);

					Thread?.NewState("Concentrator");
					this.concentratorClient = new ConcentratorClient(this.xmppClient);

					Thread?.NewState("PEP");
					this.pepClient = new PepClient(this.xmppClient);
					this.ReregisterPepEventHandlers(this.pepClient);

					Thread?.NewState("HTTPX");
					this.httpxClient = new HttpxClient(this.xmppClient, 8192);
					Types.SetModuleParameter("XMPP", this.xmppClient);      // Makes the XMPP Client the default XMPP client, when resolving HTTP over XMPP requests.

					Thread?.NewState("Connect");
					this.IsLoggedOut = false;
					this.xmppClient.Connect(IsIpAddress ? string.Empty : this.domainName);
					this.RecreateReconnectTimer();

					// Await connected state during registration or user initiated log in, but not otherwise.
					if (!this.TagProfile.IsCompleteOrWaitingForValidation())
					{
						Thread?.NewState("Wait");
						if (!await this.WaitForConnectedState(Constants.Timeouts.XmppConnect))
						{
							this.LogService.LogWarning("Connection to XMPP server failed.",
								new KeyValuePair<string, object>("Domain", this.domainName),
								new KeyValuePair<string, object>("Account", this.accountName),
								new KeyValuePair<string, object>("Timeout", Constants.Timeouts.XmppConnect));
						}
					}
				}
			}
			finally
			{
				this.isCreatingClient = false;
			}
		}

		private async Task DestroyXmppClient()
		{
			this.reconnectTimer?.Dispose();
			this.reconnectTimer = null;

			await this.OnConnectionStateChanged(XmppState.Offline);

			if (this.xmppEventSink is not null)
			{
				this.LogService.RemoveListener(this.xmppEventSink);
				this.xmppEventSink.Dispose();
				this.xmppEventSink = null;
			}

			this.contractsClient?.Dispose();
			this.contractsClient = null;

			this.fileUploadClient?.Dispose();
			this.fileUploadClient = null;

			this.mucClient?.Dispose();
			this.mucClient = null;

			this.thingRegistryClient?.Dispose();
			this.thingRegistryClient = null;

			this.provisioningClient?.Dispose();
			this.provisioningClient = null;

			this.eDalerClient?.Dispose();
			this.eDalerClient = null;

			this.neuroFeaturesClient?.Dispose();
			this.neuroFeaturesClient = null;

			this.pushNotificationClient?.Dispose();
			this.pushNotificationClient = null;

			this.sensorClient?.Dispose();
			this.sensorClient = null;

			this.controlClient?.Dispose();
			this.controlClient = null;

			this.concentratorClient?.Dispose();
			this.concentratorClient = null;

			this.pepClient?.Dispose();
			this.pepClient = null;

			this.abuseClient?.Dispose();
			this.abuseClient = null;

			this.xmppClient?.Dispose();
			this.xmppClient = null;
		}

		private bool XmppStale()
		{
			return this.xmppClient is null ||
				this.xmppClient.State == XmppState.Offline ||
				this.xmppClient.State == XmppState.Error ||
				(this.xmppClient.State != XmppState.Connected && (DateTime.Now - this.xmppLastStateChange).TotalSeconds >= 10);
		}

		private bool XmppParametersCurrent()
		{
			if (this.xmppClient is null)
				return false;

			if (this.domainName != this.TagProfile.Domain)
				return false;

			if (this.accountName != this.TagProfile.Account)
				return false;

			if (this.passwordHash != this.TagProfile.PasswordHash)
				return false;

			if (this.passwordHashMethod != this.TagProfile.PasswordHashMethod)
				return false;

			if (this.contractsClient?.ComponentAddress != this.TagProfile.LegalJid)
				return false;

			if (this.fileUploadClient?.FileUploadJid != this.TagProfile.HttpFileUploadJid)
				return false;

			if (this.mucClient?.ComponentAddress != this.TagProfile.MucJid)
				return false;

			if (this.thingRegistryClient?.ThingRegistryAddress != this.TagProfile.RegistryJid)
				return false;

			if (this.provisioningClient?.ProvisioningServerAddress != this.TagProfile.ProvisioningJid)
				return false;

			if (this.eDalerClient?.ComponentAddress != this.TagProfile.EDalerJid)
				return false;

			if (this.neuroFeaturesClient?.ComponentAddress != this.TagProfile.NeuroFeaturesJid)
				return false;

			if ((this.pushNotificationClient is null) ^ !(this.TagProfile.SupportsPushNotification.HasValue && this.TagProfile.SupportsPushNotification.Value))
				return false;

			return true;
		}

		private bool ShouldCreateClient()
		{
			return this.TagProfile.Step > RegistrationStep.Account && !this.XmppParametersCurrent();
		}

		private void RecreateReconnectTimer()
		{
			this.reconnectTimer?.Dispose();
			this.reconnectTimer = new Timer(this.ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);
		}

		#endregion

		#region Lifecycle

		public async Task<bool> WaitForConnectedState(TimeSpan Timeout)
		{
			if (this.xmppClient is null)
			{
				DateTime Start = DateTime.Now;

				while (this.xmppClient is null && DateTime.Now - Start < Timeout)
					await Task.Delay(1000);

				if (this.xmppClient is null)
					return false;

				Timeout -= DateTime.Now - Start;
			}

			if (this.xmppClient.State == XmppState.Connected)
				return true;

			if (Timeout < TimeSpan.Zero)
				return false;

			int i = await this.xmppClient.WaitStateAsync((int)Timeout.TotalMilliseconds, XmppState.Connected);
			return i >= 0;
		}

		public override async Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			ProfilerThread Thread = this.startupProfiler?.CreateThread("XMPP", ProfilerThreadType.Sequential);
			Thread?.Start();
			try
			{
				if (this.BeginLoad(cancellationToken))
				{
					Thread?.NewState("Load");
					try
					{
						this.TagProfile.StepChanged += this.TagProfile_StepChanged;

						if (this.ShouldCreateClient())
						{
							Thread?.NewState("XMPP");

							ProfilerThread ClientsThread = Thread?.CreateSubThread("Clients", ProfilerThreadType.Sequential);
							ClientsThread?.Start();
							try
							{
								await this.CreateXmppClient(this.TagProfile.Step <= RegistrationStep.RegisterIdentity, ClientsThread);
							}
							finally
							{
								ClientsThread?.Stop();
							}
						}

						if ((this.xmppClient is not null) &&
							this.xmppClient.State == XmppState.Connected &&
							this.TagProfile.IsCompleteOrWaitingForValidation())
						{
							Thread?.NewState("Presence");
							// Don't await this one, just fire and forget, to improve startup time.
							_ = this.xmppClient.SetPresenceAsync(Availability.Online);
						}

						Thread?.NewState("End");
						this.EndLoad(true);
					}
					catch (Exception ex)
					{
						ex = Log.UnnestException(ex);
						Thread?.Exception(ex);
						this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
						this.EndLoad(false);
					}
				}
			}
			finally
			{
				Thread?.Stop();
			}
		}

		public override Task Unload()
		{
			return this.Unload(false);
		}

		public Task UnloadFast()
		{
			return this.Unload(true);
		}

		private async Task Unload(bool fast)
		{
			if (this.BeginUnload())
			{
				try
				{
					this.TagProfile.StepChanged -= this.TagProfile_StepChanged;

					this.reconnectTimer?.Dispose();
					this.reconnectTimer = null;

					if (this.xmppClient is not null)
					{
						this.xmppClient.CheckConnection = false;

						if (!fast)
						{
							try
							{
								await Task.WhenAny(new Task[]
								{
									this.xmppClient.SetPresenceAsync(Availability.Offline),
									Task.Delay(1000)    // Wait at most 1000 ms.
								});
							}
							catch (Exception)
							{
								// Ignore
							}
						}
					}

					await this.DestroyXmppClient();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				this.EndUnload();
			}
		}

		private async Task TagProfile_StepChanged(object Sender, EventArgs e)
		{
			if (!this.IsLoaded)
				return;

			if (this.ShouldCreateClient())
				await this.CreateXmppClient(this.TagProfile.Step <= RegistrationStep.RegisterIdentity, null);
			else if (this.TagProfile.Step <= RegistrationStep.Account)
				await this.DestroyXmppClient();
		}

		private Task XmppClient_Error(object Sender, Exception e)
		{
			this.LatestError = e.Message;
			return Task.CompletedTask;
		}

		private Task XmppClient_ConnectionError(object Sender, Exception e)
		{
			if (e is ObjectDisposedException)
				this.LatestConnectionError = LocalizationResourceManager.Current["UnableToConnect"];
			else
				this.LatestConnectionError = e.Message;

			return Task.CompletedTask;
		}

		private async Task XmppClient_StateChanged(object Sender, XmppState NewState)
		{
			this.xmppLastStateChange = DateTime.Now;

			this.xmppThread?.NewState(NewState.ToString());

			switch (NewState)
			{
				case XmppState.Connecting:
					this.LatestError = string.Empty;
					this.LatestConnectionError = string.Empty;
					break;

				case XmppState.Connected:
					this.LatestError = string.Empty;
					this.LatestConnectionError = string.Empty;

					this.xmppConnected = true;

					this.RecreateReconnectTimer();

					if (string.IsNullOrEmpty(this.TagProfile.PasswordHashMethod))
						await this.TagProfile.SetAccount(this.TagProfile.Account, this.xmppClient.PasswordHash, this.xmppClient.PasswordHashMethod);

					if (this.TagProfile.NeedsUpdating() && await this.DiscoverServices())
					{
						if (this.contractsClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
						{
							this.contractsClient = new ContractsClient(this.xmppClient, this.TagProfile.LegalJid);
							this.RegisterContractsEventHandlers();

							if (!await this.contractsClient.LoadKeys(false))
							{
								this.contractsClient.Dispose();
								this.contractsClient = null;
							}
						}

						if (this.fileUploadClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.HttpFileUploadJid) && this.TagProfile.HttpFileUploadMaxSize.HasValue)
							this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, this.TagProfile.HttpFileUploadJid, this.TagProfile.HttpFileUploadMaxSize);

						if (this.mucClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.MucJid))
							this.mucClient = new MultiUserChatClient(this.xmppClient, this.TagProfile.MucJid);

						if (this.thingRegistryClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.RegistryJid))
							this.thingRegistryClient = new ThingRegistryClient(this.xmppClient, this.TagProfile.RegistryJid);

						if (this.provisioningClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.RegistryJid))
						{
							this.provisioningClient = new ProvisioningClient(this.xmppClient, this.TagProfile.ProvisioningJid)
							{
								ManagePresenceSubscriptionRequests = false
							};

							this.provisioningClient.CanControlQuestion += this.ProvisioningClient_CanControlQuestion;
							this.provisioningClient.CanReadQuestion += this.ProvisioningClient_CanReadQuestion;
							this.provisioningClient.IsFriendQuestion += this.ProvisioningClient_IsFriendQuestion;
						}

						if (this.eDalerClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.EDalerJid))
						{
							this.eDalerClient = new EDalerClient(this.xmppClient, this.contractsClient, this.TagProfile.EDalerJid);
							this.RegisterEDalerEventHandlers(this.eDalerClient);
						}

						if (this.neuroFeaturesClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.NeuroFeaturesJid))
						{
							this.neuroFeaturesClient = new NeuroFeaturesClient(this.xmppClient, this.contractsClient, this.TagProfile.NeuroFeaturesJid);
							this.RegisterNeuroFeatureEventHandlers(this.neuroFeaturesClient);
						}

						if (this.pushNotificationClient is null && this.TagProfile.SupportsPushNotification.HasValue && this.TagProfile.SupportsPushNotification.Value)
							this.pushNotificationClient = new PushNotificationClient(this.xmppClient);
					}

					this.LogService.AddListener(this.xmppEventSink);

					await this.PushNotificationService.CheckPushNotificationToken();

					this.xmppThread?.Stop();
					this.xmppThread = null;
					this.startupProfiler = null;
					break;

				case XmppState.Offline:
				case XmppState.Error:
					if (this.xmppConnected && !this.IsUnloading)
					{
						this.xmppConnected = false;

						if (this.xmppClient is not null && !this.xmppClient.Disposed)
							this.xmppClient.Reconnect();
					}

					this.xmppThread?.Stop();
					this.xmppThread = null;
					this.startupProfiler = null;
					break;
			}

			await this.OnConnectionStateChanged(NewState);
		}

		/// <summary>
		/// An event that triggers whenever the connection state to the XMPP server changes.
		/// </summary>
		public event StateChangedEventHandler ConnectionStateChanged;

		private async Task OnConnectionStateChanged(XmppState NewState)
		{
			try
			{
				Task T = this.ConnectionStateChanged?.Invoke(this, NewState);
				if (T is not null)
					await T;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		#endregion

		#region State

		public bool IsLoggedOut { get; private set; }
		public bool IsOnline => (this.xmppClient is not null) && this.xmppClient.State == XmppState.Connected;
		public XmppState State => this.xmppClient?.State ?? XmppState.Offline;
		public string BareJid => this.xmppClient?.BareJID ?? string.Empty;

		public string LatestError { get; private set; }
		public string LatestConnectionError { get; private set; }

		#endregion

		#region Connections

		private enum ConnectOperation
		{
			Connect,
			ConnectAndCreateAccount,
			ConnectToAccount
		}

		public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, bool isIpAddress, string hostName, int portNumber,
			string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
		{
			return this.TryConnectInner(domain, isIpAddress, hostName, portNumber, string.Empty, string.Empty, string.Empty, languageCode,
				string.Empty, string.Empty, applicationAssembly, connectedFunc, ConnectOperation.Connect);
		}

		public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, bool isIpAddress, string hostName,
			int portNumber, string userName, string password, string languageCode, string ApiKey, string ApiSecret,
			Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
		{
			return this.TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, string.Empty, languageCode,
				ApiKey, ApiSecret, applicationAssembly, connectedFunc, ConnectOperation.ConnectAndCreateAccount);
		}

		public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, bool isIpAddress, string hostName,
			int portNumber, string userName, string password, string passwordMethod, string languageCode, Assembly applicationAssembly,
			Func<XmppClient, Task> connectedFunc)
		{
			return this.TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, passwordMethod, languageCode,
				string.Empty, string.Empty, applicationAssembly, connectedFunc, ConnectOperation.ConnectToAccount);
		}

		private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, bool isIpAddress, string hostName,
			int portNumber, string userName, string password, string passwordMethod, string languageCode, string ApiKey, string ApiSecret,
			Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
		{
			TaskCompletionSource<bool> connected = new();
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
				if (e is ObjectDisposedException)
					connectionError = LocalizationResourceManager.Current["UnableToConnect"];
				else
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
							connected.TrySetResult(true);
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

			XmppClient client = null;
			try
			{
				if (string.IsNullOrEmpty(passwordMethod))
					client = new XmppClient(hostName, portNumber, userName, password, languageCode, applicationAssembly, this.sniffer);
				else
					client = new XmppClient(hostName, portNumber, userName, password, passwordMethod, languageCode, applicationAssembly, this.sniffer);

				if (operation == ConnectOperation.ConnectAndCreateAccount)
				{
					if (!string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ApiSecret))
						client.AllowRegistration(ApiKey, ApiSecret);
					else
						client.AllowRegistration();
				}

				client.TrustServer = !isIpAddress;
				client.AllowCramMD5 = false;
				client.AllowDigestMD5 = false;
				client.AllowPlain = false;
				client.AllowEncryption = true;
				client.AllowScramSHA1 = true;
				client.AllowScramSHA256 = true;
				client.AllowQuickLogin = true;

				client.OnConnectionError += OnConnectionError;
				client.OnStateChanged += OnStateChanged;

				client.Connect(isIpAddress ? string.Empty : domain);

				void TimerCallback(object _)
				{
					timeout = true;
					connected.TrySetResult(false);
				}

				using (Timer _ = new(TimerCallback, null, (int)Constants.Timeouts.XmppConnect.TotalMilliseconds, Timeout.Infinite))
				{
					succeeded = await connected.Task;
				}

				if (succeeded && (connectedFunc is not null))
					await connectedFunc(client);

				client.OnStateChanged -= OnStateChanged;
				client.OnConnectionError -= OnConnectionError;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex, new KeyValuePair<string, object>(nameof(ConnectOperation), operation.ToString()));
				succeeded = false;
				errorMessage = string.Format(LocalizationResourceManager.Current["UnableToConnectTo"], domain);
			}
			finally
			{
				client?.Dispose();
				client = null;
			}

			if (!succeeded && string.IsNullOrEmpty(errorMessage))
			{
				System.Diagnostics.Debug.WriteLine("Sniffer: ", this.sniffer.SnifferToText());

				if (!streamNegotiation || timeout)
					errorMessage = string.Format(LocalizationResourceManager.Current["CantConnectTo"], domain);
				else if (!streamOpened)
					errorMessage = string.Format(LocalizationResourceManager.Current["DomainIsNotAValidOperator"], domain);
				else if (!startingEncryption)
					errorMessage = string.Format(LocalizationResourceManager.Current["DomainDoesNotFollowEncryptionPolicy"], domain);
				else if (!authenticating)
					errorMessage = string.Format(LocalizationResourceManager.Current["UnableToAuthenticateWith"], domain);
				else if (!registering)
				{
					if (!string.IsNullOrWhiteSpace(connectionError))
						errorMessage = connectionError;
					else
						errorMessage = string.Format(LocalizationResourceManager.Current["OperatorDoesNotSupportRegisteringNewAccounts"], domain);
				}
				else if (operation == ConnectOperation.ConnectAndCreateAccount)
					errorMessage = string.Format(LocalizationResourceManager.Current["AccountNameAlreadyTaken"], this.accountName);
				else if (operation == ConnectOperation.ConnectToAccount)
					errorMessage = string.Format(LocalizationResourceManager.Current["InvalidUsernameOrPassword"], this.accountName);
				else
					errorMessage = string.Format(LocalizationResourceManager.Current["UnableToConnectTo"], domain);
			}

			return (succeeded, errorMessage);
		}

		private void ReconnectTimer_Tick(object _)
		{
			if (this.xmppClient is null)
				return;

			if (!this.NetworkService.IsOnline)
				return;

			if (this.XmppStale())
			{
				this.xmppLastStateChange = DateTime.Now;

				if (!this.xmppClient.Disposed)
					this.xmppClient.Reconnect();
			}
		}

		#endregion

		#region Password

		/// <summary>
		/// Changes the password of the account.
		/// </summary>
		/// <param name="NewPassword">New password</param>
		/// <returns>If change was successful.</returns>
		public Task<bool> ChangePassword(string NewPassword)
		{
			TaskCompletionSource<bool> PasswordChanged = new();

			this.xmppClient.ChangePassword(NewPassword, (sender, e) =>
			{
				PasswordChanged.TrySetResult(e.Ok);
				return Task.CompletedTask;
			}, null);

			return PasswordChanged.Task;
		}

		#endregion

		#region Components & Services

		/// <summary>
		/// Performs a Service Discovery on a remote entity.
		/// </summary>
		/// <param name="FullJid">Full JID of entity.</param>
		/// <returns>Service Discovery response.</returns>
		public Task<ServiceDiscoveryEventArgs> SendServiceDiscoveryRequest(string FullJid)
		{
			TaskCompletionSource<ServiceDiscoveryEventArgs> Result = new();

			this.xmppClient.SendServiceDiscoveryRequest(FullJid, (_, e) =>
			{
				Result.TrySetResult(e);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Run this method to discover services for any given XMPP server.
		/// </summary>
		/// <param name="Client">The client to use. Can be <c>null</c>, in which case the default is used.</param>
		/// <returns>If TAG services were found.</returns>
		public async Task<bool> DiscoverServices(XmppClient Client = null)
		{
			Client ??= this.xmppClient;
			if (Client is null)
				return false;

			ServiceItemsDiscoveryEventArgs response;

			try
			{
				response = await Client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);
			}
			catch (Exception ex)
			{
				string commsDump = await this.sniffer.SnifferToText();
				this.LogService.LogException(ex, new KeyValuePair<string, object>("Sniffer", commsDump));
				return false;
			}

			List<Task> Tasks = new();
			object SynchObject = new();

			Tasks.Add(this.CheckFeatures(Client, SynchObject));

			foreach (Item Item in response.Items)
				Tasks.Add(this.CheckComponent(Client, Item, SynchObject));

			await Task.WhenAll(Tasks.ToArray());

			if (string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.TagProfile.HttpFileUploadJid) || !this.TagProfile.HttpFileUploadMaxSize.HasValue)
				return false;

			if (string.IsNullOrWhiteSpace(this.TagProfile.LogJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.TagProfile.MucJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.TagProfile.EDalerJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.TagProfile.NeuroFeaturesJid))
				return false;

			if (!(this.TagProfile.SupportsPushNotification.HasValue && this.TagProfile.SupportsPushNotification.Value))
				return false;

			return true;
		}

		private async Task CheckFeatures(XmppClient Client, object SynchObject)
		{
			ServiceDiscoveryEventArgs e = await Client.ServiceDiscoveryAsync(string.Empty);

			lock (SynchObject)
			{
				this.TagProfile.SetSupportsPushNotification(e.HasFeature(PushNotificationClient.MessagePushNamespace));
			}
		}

		private async Task CheckComponent(XmppClient Client, Item Item, object SynchObject)
		{
			ServiceDiscoveryEventArgs itemResponse = await Client.ServiceDiscoveryAsync(null, Item.JID, Item.Node);

			lock (SynchObject)
			{
				if (itemResponse.HasFeature(ContractsClient.NamespaceLegalIdentities))
					this.TagProfile.SetLegalJid(Item.JID);

				if (itemResponse.HasFeature(ThingRegistryClient.NamespaceDiscovery))
					this.TagProfile.SetRegistryJid(Item.JID);

				if (itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningDevice) &&
					itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningOwner) &&
					itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningToken))
				{
					this.TagProfile.SetProvisioningJid(Item.JID);
				}

				if (itemResponse.HasFeature(HttpFileUploadClient.Namespace))
				{
					long? maxSize = HttpFileUploadClient.FindMaxFileSize(Client, itemResponse);
					this.TagProfile.SetFileUploadParameters(Item.JID, maxSize);
				}

				if (itemResponse.HasFeature(XmppEventSink.NamespaceEventLogging))
					this.TagProfile.SetLogJid(Item.JID);

				if (itemResponse.HasFeature(MultiUserChatClient.NamespaceMuc))
					this.TagProfile.SetMucJid(Item.JID);

				if (itemResponse.HasFeature(EDalerClient.NamespaceEDaler))
					this.TagProfile.SetEDalerJid(Item.JID);

				if (itemResponse.HasFeature(NeuroFeaturesClient.NamespaceNeuroFeatures))
					this.TagProfile.SetNeuroFeaturesJid(Item.JID);
			}
		}

		#endregion

		#region Transfer

		private async Task TransferIdDelivered(object Sender, MessageEventArgs e)
		{
			if (e.From != Constants.Domains.OnboardingDomain)
				return;

			string Code = XML.Attribute(e.Content, "code");
			bool Deleted = XML.Attribute(e.Content, "deleted", false);

			if (!Deleted)
				return;

			string CodesGenerated = await RuntimeSettings.GetAsync("TransferId.CodesSent", string.Empty);
			string[] Codes = CodesGenerated.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);

			if (Array.IndexOf<string>(Codes, Code) < 0)
				return;

			await this.DestroyXmppClient();

			this.domainName = string.Empty;
			this.accountName = string.Empty;
			this.passwordHash = string.Empty;
			this.passwordHashMethod = string.Empty;
			this.xmppConnected = false;

			this.TagProfile.ClearAll();
			await RuntimeSettings.SetAsync("TransferId.CodesSent", string.Empty);
			await Database.Provider.Flush();

			this.UiSerializer.BeginInvokeOnMainThread(async () => await App.Current.SetRegistrationPageAsync());
		}

		/// <summary>
		/// Registers a Transfer ID Code
		/// </summary>
		/// <param name="Code">Transfer Code</param>
		public async Task AddTransferCode(string Code)
		{
			string CodesGenerated = await RuntimeSettings.GetAsync("TransferId.CodesSent", string.Empty);

			if (string.IsNullOrEmpty(CodesGenerated))
				CodesGenerated = Code;
			else
				CodesGenerated += "\r\n" + Code;

			await RuntimeSettings.SetAsync("TransferId.CodesSent", CodesGenerated);
			await Database.Provider.Flush();
		}

		#endregion

		#region Presence Subscriptions

		private async Task XmppClient_OnPresenceSubscribe(object Sender, PresenceEventArgs e)
		{
			LegalIdentity RemoteIdentity = null;
			string FriendlyName = string.IsNullOrWhiteSpace(e.NickName) ? e.FromBareJID : e.NickName;
			string PhotoUrl = null;
			int PhotoWidth = 0;
			int PhotoHeight = 0;

			foreach (XmlNode N in e.Presence.ChildNodes)
			{
				if (N is XmlElement E && E.LocalName == "identity" && E.NamespaceURI == ContractsClient.NamespaceLegalIdentities)
				{
					RemoteIdentity = LegalIdentity.Parse(E);
					if (RemoteIdentity is not null)
					{
						FriendlyName = ContactInfo.GetFriendlyName(RemoteIdentity);

						IdentityStatus Status = await this.contractsClient.ValidateAsync(RemoteIdentity);
						if (Status != IdentityStatus.Valid)
						{
							e.Decline();

							Log.Warning("Invalid ID received. Presence subscription declined.", e.FromBareJID, RemoteIdentity.Id, "IdValidationError",
								new KeyValuePair<string, object>("Recipient JID", this.BareJid),
								new KeyValuePair<string, object>("Sender JID", e.FromBareJID),
								new KeyValuePair<string, object>("Legal ID", RemoteIdentity.Id),
								new KeyValuePair<string, object>("Validation", Status));
							return;
						}

						break;
					}
				}
			}

			ContactInfo Info = await ContactInfo.FindByBareJid(e.FromBareJID);
			if ((Info is not null) && Info.AllowSubscriptionFrom.HasValue)
			{
				if (Info.AllowSubscriptionFrom.Value)
					e.Accept();
				else
					e.Decline();

				if (Info.FriendlyName != FriendlyName || ((RemoteIdentity is not null) && Info.LegalId != RemoteIdentity.Id))
				{
					if (RemoteIdentity is not null)
					{
						Info.LegalId = RemoteIdentity.Id;
						Info.LegalIdentity = RemoteIdentity;
					}

					Info.FriendlyName = FriendlyName;
					await Database.Update(Info);
				}

				return;
			}

			if ((RemoteIdentity is not null) && (RemoteIdentity.Attachments is not null))
				(PhotoUrl, PhotoWidth, PhotoHeight) = await PhotosLoader.LoadPhotoAsTemporaryFile(RemoteIdentity.Attachments, 300, 300);

			SubscriptionRequestPopupPage SubscriptionRequestPage = new(e.FromBareJID, FriendlyName, PhotoUrl, PhotoWidth, PhotoHeight);

			await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(SubscriptionRequestPage);
			PresenceRequestAction Action = await SubscriptionRequestPage.Result;

			switch (Action)
			{
				case PresenceRequestAction.Accept:
					e.Accept();

					if (Info is null)
					{
						Info = new ContactInfo()
						{
							AllowSubscriptionFrom = true,
							BareJid = e.FromBareJID,
							FriendlyName = string.IsNullOrWhiteSpace(e.NickName) ? e.FromBareJID : e.NickName,
							IsThing = false
						};

						await Database.Insert(Info);
					}
					else if (!Info.AllowSubscriptionFrom.HasValue || !Info.AllowSubscriptionFrom.Value)
					{
						Info.AllowSubscriptionFrom = true;
						await Database.Update(Info);
					}

					RosterItem Item = this.xmppClient[e.FromBareJID];

					if (Item is null || (Item.State != SubscriptionState.Both && Item.State != SubscriptionState.To))
					{
						SubscribeToPopupPage SubscribeToPage = new(e.FromBareJID);

						await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(SubscribeToPage);
						bool? SubscribeTo = await SubscribeToPage.Result;

						if (SubscribeTo.HasValue && SubscribeTo.Value)
						{
							string IdXml;

							if (this.TagProfile.LegalIdentity is null)
								IdXml = string.Empty;
							else
							{
								StringBuilder Xml = new();
								this.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
								IdXml = Xml.ToString();
							}

							e.Client.RequestPresenceSubscription(e.FromBareJID, IdXml);
						}
					}
					break;

				case PresenceRequestAction.Reject:
					e.Decline();

					ReportOrBlockPopupPage ReportOrBlockPage = new(e.FromBareJID);

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(ReportOrBlockPage);
					ReportOrBlockAction ReportOrBlock = await ReportOrBlockPage.Result;

					if (ReportOrBlock == ReportOrBlockAction.Block || ReportOrBlock == ReportOrBlockAction.Report)
					{
						if (Info is null)
						{
							Info = new ContactInfo()
							{
								AllowSubscriptionFrom = false,
								BareJid = e.FromBareJID,
								FriendlyName = string.IsNullOrWhiteSpace(e.NickName) ? e.FromBareJID : e.NickName,
								IsThing = false
							};

							await Database.Insert(Info);
						}
						else if (!Info.AllowSubscriptionFrom.HasValue || Info.AllowSubscriptionFrom.Value)
						{
							Info.AllowSubscriptionFrom = false;
							await Database.Update(Info);
						}

						if (ReportOrBlock == ReportOrBlockAction.Report)
						{
							ReportTypePopupPage ReportTypePage = new(e.FromBareJID);

							await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(ReportOrBlockPage);
							ReportingReason? ReportType = await ReportTypePage.Result;

							if (ReportType.HasValue)
							{
								TaskCompletionSource<bool> Result = new();

								await this.abuseClient.BlockJID(e.FromBareJID, ReportType.Value, (sender2, e2) =>
								{
									Result.TrySetResult(e.Ok);
									return Task.CompletedTask;
								}, null);

								await Result.Task;
							}
						}
					}
					break;

				case PresenceRequestAction.Ignore:
				default:
					break;
			}
		}

		private async Task XmppClient_OnPresenceUnsubscribed(object Sender, PresenceEventArgs e)
		{
			ContactInfo ContactInfo = await ContactInfo.FindByBareJid(e.FromBareJID);
			if ((ContactInfo is not null) && ContactInfo.AllowSubscriptionFrom.HasValue && ContactInfo.AllowSubscriptionFrom.Value)
			{
				ContactInfo.AllowSubscriptionFrom = null;
				await Database.Update(ContactInfo);
			}
		}

		#endregion

		#region IQ Stanzas (Information Query)

		/// <summary>
		/// Performs an asynchronous IQ Set request/response operation.
		/// </summary>
		/// <param name="To">Destination address</param>
		/// <param name="Xml">XML to embed into the request.</param>
		/// <returns>Response XML element.</returns>
		/// <exception cref="TimeoutException">If a timeout occurred.</exception>
		/// <exception cref="XmppException">If an IQ error is returned.</exception>
		public Task<XmlElement> IqSetAsync(string To, string Xml)
		{
			return this.xmppClient.IqSetAsync(To, Xml);
		}

		#endregion

		#region Messages

		/// <summary>
		/// Sends a message
		/// </summary>
		/// <param name="QoS">Quality of Service level of message.</param>
		/// <param name="Type">Type of message to send.</param>
		/// <param name="Id">Message ID</param>
		/// <param name="To">Destination address</param>
		/// <param name="CustomXml">Custom XML</param>
		/// <param name="Body">Body text of chat message.</param>
		/// <param name="Subject">Subject</param>
		/// <param name="Language">Language used.</param>
		/// <param name="ThreadId">Thread ID</param>
		/// <param name="ParentThreadId">Parent Thread ID</param>
		/// <param name="DeliveryCallback">Callback to call when message has been sent, or failed to be sent.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void SendMessage(QoSLevel QoS, Waher.Networking.XMPP.MessageType Type, string Id, string To, string CustomXml, string Body,
			string Subject, string Language, string ThreadId, string ParentThreadId, DeliveryEventHandler DeliveryCallback, object State)
		{
			this.xmppClient.SendMessage(QoS, Type, Id, To, CustomXml, Body, Subject, Language, ThreadId, ParentThreadId, DeliveryCallback, State);
			// TODO: End-to-End encryption
		}

		private Task XmppClient_OnNormalMessage(object Sender, MessageEventArgs e)
		{
			Log.Warning("Unhandled message received.", e.To, e.From,
				new KeyValuePair<string, object>("Stanza", e.Message.OuterXml));

			return Task.CompletedTask;
		}

		private async Task XmppClient_OnChatMessage(object Sender, MessageEventArgs e)
		{
			string RemoteBareJid = e.FromBareJID;

			foreach (XmlNode N in e.Message.ChildNodes)
			{
				if (N is XmlElement E &&
					E.LocalName == "qlRef" &&
					E.NamespaceURI == XmppClient.NamespaceQuickLogin &&
					RemoteBareJid.IndexOf('@') < 0 &&
					RemoteBareJid.IndexOf('/') < 0)
				{
					LegalIdentity RemoteIdentity = null;

					foreach (XmlNode N2 in E.ChildNodes)
					{
						if (N2 is XmlElement E2 &&
							E2.LocalName == "identity" &&
							E2.NamespaceURI == ContractsClient.NamespaceLegalIdentities)
						{
							RemoteIdentity = LegalIdentity.Parse(E2);
							break;
						}
					}

					if (RemoteIdentity is not null)
					{
						IdentityStatus Status = await this.ValidateIdentity(RemoteIdentity);
						if (Status != IdentityStatus.Valid)
						{
							Log.Warning("Message rejected because the embedded legal identity was not valid.",
								new KeyValuePair<string, object>("Identity", RemoteIdentity.Id),
								new KeyValuePair<string, object>("From", RemoteBareJid),
								new KeyValuePair<string, object>("Status", Status));
							return;
						}

						string Jid = RemoteIdentity["JID"];

						if (string.IsNullOrEmpty(Jid))
						{
							Log.Warning("Message rejected because the embedded legal identity lacked JID.",
								new KeyValuePair<string, object>("Identity", RemoteIdentity.Id),
								new KeyValuePair<string, object>("From", RemoteBareJid),
								new KeyValuePair<string, object>("Status", Status));
							return;
						}

						if (string.Compare(XML.Attribute(E, "bareJid", string.Empty), Jid, true) != 0)
						{
							Log.Warning("Message rejected because the embedded legal identity had a different JID compared to the JID of the quick-login reference.",
								new KeyValuePair<string, object>("Identity", RemoteIdentity.Id),
								new KeyValuePair<string, object>("From", RemoteBareJid),
								new KeyValuePair<string, object>("Status", Status));
							return;
						}

						RemoteBareJid = Jid;
					}
				}
			}

			ContactInfo ContactInfo = await ContactInfo.FindByBareJid(RemoteBareJid);
			string FriendlyName = ContactInfo?.FriendlyName ?? RemoteBareJid;
			string ReplaceObjectId = null;

			ChatMessage Message = new()
			{
				Created = DateTime.UtcNow,
				RemoteBareJid = RemoteBareJid,
				RemoteObjectId = e.Id,
				MessageType = Messages.MessageType.Received,
				Html = string.Empty,
				PlainText = e.Body,
				Markdown = string.Empty
			};

			foreach (XmlNode N in e.Message.ChildNodes)
			{
				if (N is XmlElement E)
				{
					switch (N.LocalName)
					{
						case "content":
							if (E.NamespaceURI == "urn:xmpp:content")
							{
								string Type = XML.Attribute(E, "type");

								switch (Type)
								{
									case "text/markdown":
										Message.Markdown = E.InnerText;
										break;

									case "text/plain":
										Message.PlainText = E.InnerText;
										break;

									case "text/html":
										Message.Html = E.InnerText;
										break;
								}
							}
							break;

						case "html":
							if (E.NamespaceURI == "http://jabber.org/protocol/xhtml-im")
							{
								string Html = E.InnerXml;

								int i = Html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
								if (i >= 0)
								{
									i = Html.IndexOf('>', i + 5);
									if (i >= 0)
										Html = Html[(i + 1)..].TrimStart();

									i = Html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
									if (i >= 0)
										Html = Html[..i].TrimEnd();
								}

								Message.Html = Html;
							}
							break;

						case "replace":
							if (E.NamespaceURI == "urn:xmpp:message-correct:0")
								ReplaceObjectId = XML.Attribute(E, "id");
							break;

						case "delay":
							if (E.NamespaceURI == PubSubClient.NamespaceDelayedDelivery &&
								E.HasAttribute("stamp") &&
								XML.TryParse(E.GetAttribute("stamp"), out DateTime Timestamp2))
							{
								Message.Created = Timestamp2.ToUniversalTime();
							}
							break;
					}
				}
			}

			if (!string.IsNullOrEmpty(Message.Markdown))
			{
				try
				{
					MarkdownSettings Settings = new()
					{
						AllowScriptTag = false,
						EmbedEmojis = false,    // TODO: Emojis
						AudioAutoplay = false,
						AudioControls = false,
						ParseMetaData = false,
						VideoAutoplay = false,
						VideoControls = false
					};

					MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Message.Markdown, Settings);

					if (string.IsNullOrEmpty(Message.PlainText))
						Message.PlainText = (await Doc.GeneratePlainText()).Trim();

					if (string.IsNullOrEmpty(Message.Html))
						Message.Html = HtmlDocument.GetBody(await Doc.GenerateHTML());
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					Message.Markdown = string.Empty;
				}
			}

			if (string.IsNullOrEmpty(ReplaceObjectId))
				await Database.Insert(Message);
			else
			{
				ChatMessage Old = await Database.FindFirstIgnoreRest<ChatMessage>(new FilterAnd(
					new FilterFieldEqualTo("RemoteBareJid", RemoteBareJid),
					new FilterFieldEqualTo("RemoteObjectId", ReplaceObjectId)));

				if (Old is null)
				{
					ReplaceObjectId = null;
					await Database.Insert(Message);
				}
				else
				{
					Old.Updated = Message.Created;
					Old.Html = Message.Html;
					Old.PlainText = Message.PlainText;
					Old.Markdown = Message.Markdown;

					await Database.Update(Old);

					Message = Old;
				}
			}

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				INavigationService NavigationService = App.Instantiate<INavigationService>();

				if ((NavigationService.CurrentPage is ChatPage || NavigationService.CurrentPage is ChatPageIos) &&
					NavigationService.CurrentPage.BindingContext is ChatViewModel ChatViewModel &&
					string.Compare(ChatViewModel.BareJid, RemoteBareJid, true) == 0)
				{
					if (string.IsNullOrEmpty(ReplaceObjectId))
						await ChatViewModel.MessageAddedAsync(Message);
					else
						await ChatViewModel.MessageUpdatedAsync(Message);
				}
				else
				{
					await this.NotificationService.NewEvent(new ChatMessageNotificationEvent(e, RemoteBareJid)
					{
						ReplaceObjectId = ReplaceObjectId,
						BareJid = RemoteBareJid,
						Category = RemoteBareJid
					});
				}
			});
		}

		private Task ClientMessage(object Sender, MessageEventArgs e)
		{
			string Code = XML.Attribute(e.Content, "code");
			string Type = XML.Attribute(e.Content, "type");
			string Message = e.Body;

			if (!string.IsNullOrEmpty(Code))
			{
				try
				{
					string LocalizedMessage = LocalizationResourceManager.Current["ClientMessage" + Code];

					if (!string.IsNullOrEmpty(LocalizedMessage))
						Message = LocalizedMessage;
				}
				catch (Exception)
				{
					// Ignore
				}
			}

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				switch (Type.ToUpper())
				{
					case "NONE":
					default:
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Information"], Message,
							LocalizationResourceManager.Current["Ok"]);
						break;

					case "CLIENT":
					case "SERVER":
					case "SERVICE":
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], Message,
							LocalizationResourceManager.Current["Ok"]);
						break;

				}
			});

			return Task.CompletedTask;
		}

		#endregion

		#region Presence

		private async Task XmppClient_OnPresence(object Sender, PresenceEventArgs e)
		{
			try
			{
				Task T = this.OnPresence?.Invoke(this, e);
				if (T is not null)
					await T;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Event raised when a new presence stanza has been received.
		/// </summary>
		public event PresenceEventHandlerAsync OnPresence;

		/// <summary>
		/// Requests subscription of presence information from a contact.
		/// </summary>
		/// <param name="BareJid">Bare JID of contact.</param>
		public void RequestPresenceSubscription(string BareJid)
		{
			this.xmppClient.RequestPresenceSubscription(BareJid);
		}

		/// <summary>
		/// Requests subscription of presence information from a contact.
		/// </summary>
		/// <param name="BareJid">Bare JID of contact.</param>
		/// <param name="CustomXml">Custom XML to include in the subscription request.</param>
		public void RequestPresenceSubscription(string BareJid, string CustomXml)
		{
			this.xmppClient.RequestPresenceSubscription(BareJid, CustomXml);
		}

		/// <summary>
		/// Requests unssubscription of presence information from a contact.
		/// </summary>
		/// <param name="BareJid">Bare JID of contact.</param>
		public void RequestPresenceUnsubscription(string BareJid)
		{
			this.xmppClient.RequestPresenceUnsubscription(BareJid);
		}

		/// <summary>
		/// Requests a previous presence subscription request revoked.
		/// </summary>
		/// <param name="BareJid">Bare JID of contact.</param>
		public void RequestRevokePresenceSubscription(string BareJid)
		{
			this.xmppClient.RequestRevokePresenceSubscription(BareJid);
		}

		#endregion

		#region Roster

		/// <summary>
		/// Items in the roster.
		/// </summary>
		public RosterItem[] Roster => this.xmppClient?.Roster ?? new RosterItem[0];

		/// <summary>
		/// Gets a roster item.
		/// </summary>
		/// <param name="BareJid">Bare JID of roster item.</param>
		/// <returns>Roster item, if found, or null, if not available.</returns>
		public RosterItem GetRosterItem(string BareJid)
		{
			return this.xmppClient?.GetRosterItem(BareJid);
		}

		/// <summary>
		/// Adds an item to the roster. If an item with the same Bare JID is found in the roster, that item is updated.
		/// </summary>
		/// <param name="Item">Item to add.</param>
		public void AddRosterItem(RosterItem Item)
		{
			this.xmppClient?.AddRosterItem(Item);
		}

		/// <summary>
		/// Removes an item from the roster.
		/// </summary>
		/// <param name="BareJid">Bare JID of the roster item.</param>
		public void RemoveRosterItem(string BareJid)
		{
			this.xmppClient?.RemoveRosterItem(BareJid);
		}

		private async Task XmppClient_OnRosterItemAdded(object Sender, RosterItem Item)
		{
			try
			{
				Task T = this.OnRosterItemAdded?.Invoke(this, Item);
				if (T is not null)
					await T;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Event raised when a roster item has been added to the roster.
		/// </summary>
		public event RosterItemEventHandlerAsync OnRosterItemAdded;

		private async Task XmppClient_OnRosterItemUpdated(object Sender, RosterItem Item)
		{
			try
			{
				Task T = this.OnRosterItemUpdated?.Invoke(this, Item);
				if (T is not null)
					await T;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Event raised when a roster item has been updated in the roster.
		/// </summary>
		public event RosterItemEventHandlerAsync OnRosterItemUpdated;

		private async Task XmppClient_OnRosterItemRemoved(object Sender, RosterItem Item)
		{
			try
			{
				Task T = this.OnRosterItemRemoved?.Invoke(this, Item);
				if (T is not null)
					await T;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Event raised when a roster item has been removed from the roster.
		/// </summary>
		public event RosterItemEventHandlerAsync OnRosterItemRemoved;

		#endregion

		#region Push Notification

		/// <summary>
		/// If push notification is supported.
		/// </summary>
		public bool SupportsPushNotification => this.pushNotificationClient is not null;

		/// <summary>
		/// Registers a new token.
		/// </summary>
		/// <param name="TokenInformation">Token information.</param>
		/// <returns>If token could be registered.</returns>
		public async Task<bool> NewPushNotificationToken(TokenInformation TokenInformation)
		{
			// TODO: Check if started

			if (this.pushNotificationClient is null || !this.IsOnline)
				return false;
			else
			{
				await this.ReportNewPushNotificationToken(TokenInformation.Token, TokenInformation.Service, TokenInformation.ClientType);

				return true;
			}
		}

		/// <summary>
		/// Reports a new push-notification token to the broker.
		/// </summary>
		/// <param name="Token">Token</param>
		/// <param name="Service">Service used</param>
		/// <param name="ClientType">Client type.</param>
		public Task ReportNewPushNotificationToken(string Token, PushMessagingService Service, ClientType ClientType)
		{
			return this.pushNotificationClient.NewTokenAsync(Token, Service, ClientType);
		}

		/// <summary>
		/// Clears configured push notification rules in the broker.
		/// </summary>
		public Task ClearPushNotificationRules()
		{
			return this.pushNotificationClient.ClearRulesAsync();
		}

		/// <summary>
		/// Adds a push-notification rule in the broker.
		/// </summary>
		/// <param name="MessageType">Type of message</param>
		/// <param name="LocalName">Local name of content element</param>
		/// <param name="Namespace">Namespace of content element</param>
		/// <param name="Channel">Push-notification channel</param>
		/// <param name="MessageVariable">Variable to receive message stanza</param>
		/// <param name="PatternMatchingScript">Pattern matching script</param>
		/// <param name="ContentScript">Content script</param>
		public Task AddPushNotificationRule(Waher.Networking.XMPP.MessageType MessageType, string LocalName, string Namespace,
			string Channel, string MessageVariable, string PatternMatchingScript, string ContentScript)
		{
			return this.pushNotificationClient.AddRuleAsync(MessageType, LocalName, Namespace, Channel, MessageVariable,
				PatternMatchingScript, ContentScript);
		}

		#endregion

		#region Tokens

		/// <summary>
		/// Gets a token for use with APIs that are either distributed or use different
		/// protocols, when the client needs to authenticate itself using the current
		/// XMPP connection.
		/// </summary>
		/// <param name="Seconds">Number of seconds for which the token should be valid.</param>
		/// <returns>Token, if able to get a token, or null otherwise.</returns>
		public async Task<string> GetApiToken(int Seconds)
		{
			DateTime Now = DateTime.UtcNow;

			if (!string.IsNullOrEmpty(this.token) && Now.Subtract(this.tokenCreated).TotalSeconds < Seconds - 10)
				return this.token;

			if (!this.IsOnline)
			{
				if (!await this.WaitForConnectedState(TimeSpan.FromSeconds(20)))
					return this.token;
			}

			this.token = await this.httpxClient.GetJwtTokenAsync(Seconds);
			this.tokenCreated = Now;

			return this.token;
		}

		/// <summary>
		/// Performs an HTTP POST to a protected API on the server, over the current XMPP connection,
		/// authenticating the client using the credentials alreaedy provided over XMPP.
		/// </summary>
		/// <param name="LocalResource">Local Resource on the server to POST to.</param>
		/// <param name="Data">Data to post. This will be encoded using encoders in the type inventory.</param>
		/// <param name="Headers">Headers to provide in the POST.</param>
		/// <returns>Decoded response from the resource.</returns>
		/// <exception cref="Exception">Any communication error will be handle by raising the corresponding exception.</exception>
		public async Task<object> PostToProtectedApi(string LocalResource, object Data, params KeyValuePair<string, string>[] Headers)
		{
			StringBuilder Url = new();

			if (this.IsOnline)
				Url.Append("httpx://");
			else if (!string.IsNullOrEmpty(this.token))     // Token needs to be retrieved reegularly when connected, if protectedd APIs are to be used when disconnected or during connection.
			{
				Url.Append("https://");

				KeyValuePair<string, string> Authorization = new("Authorization", "Bearer " + this.token);

				if (Headers is null)
					Headers = new KeyValuePair<string, string>[] { Authorization };
				else
				{
					int c = Headers.Length;

					Array.Resize(ref Headers, c + 1);
					Headers[c] = Authorization;
				}
			}
			else
				throw new IOException("No connection and no token available for call to protecte API.");

			Url.Append(this.TagProfile.Domain);
			Url.Append(LocalResource);

			return await InternetContent.PostAsync(new Uri(Url.ToString()), Data, Headers);
		}

		#endregion

		#region HTTP File Upload

		/// <summary>
		/// Reference to HTTP File Upload client, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private HttpFileUploadClient FileUploadClient
		{
			get
			{
				if (this.fileUploadClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["FileUploadServiceNotFound"]);

				return this.fileUploadClient;
			}
		}

		/// <summary>
		/// Returns <c>true</c> if file upload is supported, <c>false</c> otherwise.
		/// </summary>
		public bool FileUploadIsSupported
		{
			get
			{
				try
				{
					return this.TagProfile.FileUploadIsSupported &&
						this.fileUploadClient is not null &&
						this.fileUploadClient.HasSupport;
				}
				catch (Exception ex)
				{
					this.LogService?.LogException(ex);
					return false;
				}
			}
		}

		/// <summary>
		/// Uploads a file to the upload component.
		/// </summary>
		/// <param name="FileName">Name of file.</param>
		/// <param name="ContentType">Internet content type.</param>
		/// <param name="ContentSize">Size of content.</param>
		public Task<HttpFileUploadEventArgs> RequestUploadSlotAsync(string FileName, string ContentType, long ContentSize)
		{
			return this.FileUploadClient.RequestUploadSlotAsync(FileName, ContentType, ContentSize);
		}


		#endregion

		#region Personal Eventing Protocol (PEP)

		private readonly LinkedList<KeyValuePair<Type, PersonalEventNotificationEventHandler>> pepHandlers = new();

		/// <summary>
		/// Reference to Personal Eventing Protocol (PEP) client, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private PepClient PepClient
		{
			get
			{
				if (this.pepClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["PepServiceNotFound"]);

				return this.pepClient;
			}
		}

		/// <summary>
		/// Registers an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		public void RegisterPepHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler)
		{
			lock (this.pepHandlers)
			{
				this.pepHandlers.AddLast(new KeyValuePair<Type, PersonalEventNotificationEventHandler>(PersonalEventType, Handler));
			}

			this.PepClient.RegisterHandler(PersonalEventType, Handler);
		}

		/// <summary>
		/// Unregisters an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		/// <returns>If the event handler was found and removed.</returns>
		public bool UnregisterPepHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler)
		{
			lock (this.pepHandlers)
			{
				LinkedListNode<KeyValuePair<Type, PersonalEventNotificationEventHandler>> Node = this.pepHandlers.First;

				while (Node is not null)
				{
					if (Node.Value.Key == PersonalEventType &&
						Node.Value.Value.Target.Equals(Handler.Target) &&
						Node.Value.Value.Method.Equals(Handler.Method))
					{
						this.pepHandlers.Remove(Node);
						break;
					}

					Node = Node.Next;
				}
			}

			return this.PepClient.UnregisterHandler(PersonalEventType, Handler);
		}

		private void ReregisterPepEventHandlers(PepClient PepClient)
		{
			lock (this.pepHandlers)
			{
				foreach (KeyValuePair<Type, PersonalEventNotificationEventHandler> P in this.pepHandlers)
					PepClient.RegisterHandler(P.Key, P.Value);
			}
		}

		#endregion

		#region Thing Registries & Discovery

		/// <summary>
		/// Reference to thing registry client, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private ThingRegistryClient ThingRegistryClient
		{
			get
			{
				if (this.thingRegistryClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["ThingRegistryServiceNotFound"]);

				return this.thingRegistryClient;
			}
		}

		/// <summary>
		/// JID of thing registry service.
		/// </summary>
		public string RegistryServiceJid => this.ThingRegistryClient.ThingRegistryAddress;

		/// <summary>
		/// Checks if a URI is a claim URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <returns>If <paramref name="DiscoUri"/> is a claim URI.</returns>
		public bool IsIoTDiscoClaimURI(string DiscoUri)
		{
			return ThingRegistryClient.IsIoTDiscoClaimURI(DiscoUri);
		}

		/// <summary>
		/// Checks if a URI is a search URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <returns>If <paramref name="DiscoUri"/> is a search URI.</returns>
		public bool IsIoTDiscoSearchURI(string DiscoUri)
		{
			return ThingRegistryClient.IsIoTDiscoSearchURI(DiscoUri);
		}

		/// <summary>
		/// Checks if a URI is a direct reference URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <returns>If <paramref name="DiscoUri"/> is a direct reference URI.</returns>
		public bool IsIoTDiscoDirectURI(string DiscoUri)
		{
			return ThingRegistryClient.IsIoTDiscoDirectURI(DiscoUri);
		}

		/// <summary>
		/// Tries to decode an IoTDisco Claim URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Tags">Decoded meta data tags.</param>
		/// <returns>If DiscoUri was successfully decoded.</returns>
		public bool TryDecodeIoTDiscoClaimURI(string DiscoUri, out MetaDataTag[] Tags)
		{
			return ThingRegistryClient.TryDecodeIoTDiscoClaimURI(DiscoUri, out Tags);
		}

		/// <summary>
		/// Tries to decode an IoTDisco Search URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Operators">Search operators.</param>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <returns>If the URI could be parsed.</returns>
		public bool TryDecodeIoTDiscoSearchURI(string DiscoUri, out SearchOperator[] Operators, out string RegistryJid)
		{
			RegistryJid = null;
			Operators = null;
			if (!ThingRegistryClient.TryDecodeIoTDiscoURI(DiscoUri, out IEnumerable<SearchOperator> Operators2))
				return false;

			List<SearchOperator> List = new();

			foreach (SearchOperator Operator in Operators2)
			{
				if (Operator.Name.ToUpper() == "R")
				{
					if (!string.IsNullOrEmpty(RegistryJid))
						return false;

					if (Operator is not StringTagEqualTo StrEqOp)
						return false;

					RegistryJid = StrEqOp.Value;
				}
				else
					List.Add(Operator);
			}

			Operators = List.ToArray();
			return true;
		}

		/// <summary>
		/// Tries to decode an IoTDisco Direct Reference URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Jid">JID of device</param>
		/// <param name="SourceId">Optional Source ID of device, or null if none.</param>
		/// <param name="NodeId">Optional Node ID of device, or null if none.</param>
		/// <param name="PartitionId">Optional Partition ID of device, or null if none.</param>
		/// <param name="Tags">Decoded meta data tags.</param>
		/// <returns>If the URI could be parsed.</returns>
		public bool TryDecodeIoTDiscoDirectURI(string DiscoUri, out string Jid, out string SourceId, out string NodeId,
			out string PartitionId, out MetaDataTag[] Tags)
		{
			Jid = null;
			SourceId = null;
			NodeId = null;
			PartitionId = null;

			if (!ThingRegistryClient.TryDecodeIoTDiscoURI(DiscoUri, out IEnumerable<SearchOperator> Operators2))
			{
				Tags = null;
				return false;
			}

			List<MetaDataTag> TagsFound = new();

			foreach (SearchOperator Operator in Operators2)
			{
				if (Operator is StringTagEqualTo S)
				{
					switch (S.Name.ToUpper())
					{
						case "JID":
							Jid = S.Value;
							break;

						case "SID":
							SourceId = S.Value;
							break;

						case "NID":
							NodeId = S.Value;
							break;

						case "PT":
							PartitionId = S.Value;
							break;

						default:
							TagsFound.Add(new MetaDataStringTag(S.Name, S.Value));
							break;
					}
				}
				else if (Operator is NumericTagEqualTo N)
					TagsFound.Add(new MetaDataNumericTag(N.Name, N.Value));
				else
				{
					Tags = null;
					return false;
				}
			}

			Tags = TagsFound.ToArray();
			return !string.IsNullOrEmpty(Jid);
		}

		/// <summary>
		/// Claims a think in accordance with parameters defined in a iotdisco claim URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="MakePublic">If the device should be public in the thing registry.</param>
		/// <returns>Information about the thing, or error if unable.</returns>
		public Task<NodeResultEventArgs> ClaimThing(string DiscoUri, bool MakePublic)
		{
			if (!this.TryDecodeIoTDiscoClaimURI(DiscoUri, out MetaDataTag[] Tags))
				throw new ArgumentException(LocalizationResourceManager.Current["InvalidIoTDiscoClaimUri"], nameof(DiscoUri));

			TaskCompletionSource<NodeResultEventArgs> Result = new();

			this.ThingRegistryClient.Mine(MakePublic, Tags, (sender, e) =>
			{
				Result.TrySetResult(e);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Disowns a thing
		/// </summary>
		/// <param name="RegistryJid">Registry JID</param>
		/// <param name="ThingJid">Thing JID</param>
		/// <param name="SourceId">Source ID</param>
		/// <param name="Partition">Partition</param>
		/// <param name="NodeId">Node ID</param>
		/// <returns>If the thing was disowned</returns>
		public Task<bool> Disown(string RegistryJid, string ThingJid, string SourceId, string Partition, string NodeId)
		{
			TaskCompletionSource<bool> Result = new();

			this.ThingRegistryClient.Disown(RegistryJid, ThingJid, NodeId, SourceId, Partition, (sender, e) =>
			{
				Result.TrySetResult(e.Ok);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Searches for devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <param name="DiscoUri">iotdisco URI.</param>
		/// <returns>Devices found, Registry JID, and if more devices are available.</returns>
		public async Task<(SearchResultThing[], string, bool)> Search(int Offset, int MaxCount, string DiscoUri)
		{
			if (!this.TryDecodeIoTDiscoSearchURI(DiscoUri, out SearchOperator[] Operators, out string RegistryJid))
				return (new SearchResultThing[0], RegistryJid, false);

			(SearchResultThing[] Things, bool More) = await this.Search(Offset, MaxCount, RegistryJid, Operators);

			return (Things, RegistryJid, More);
		}

		/// <summary>
		/// Searches for devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>Devices found, and if more devices are available.</returns>
		public Task<(SearchResultThing[], bool)> Search(int Offset, int MaxCount, string RegistryJid, params SearchOperator[] Operators)
		{
			TaskCompletionSource<(SearchResultThing[], bool)> Result = new();

			this.ThingRegistryClient.Search(RegistryJid, Offset, MaxCount, Operators, (sender, e) =>
			{
				if (e.Ok)
					Result.TrySetResult((e.Things, e.More));
				else
					Result.TrySetException(e.StanzaError ?? new Exception("Unable to perform search."));

				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Searches for all devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="DiscoUri">iotdisco URI.</param>
		/// <returns>Complete list of devices in registry matching the search operators, and the JID of the registry service.</returns>
		public async Task<(SearchResultThing[], string)> SearchAll(string DiscoUri)
		{
			if (!this.TryDecodeIoTDiscoSearchURI(DiscoUri, out SearchOperator[] Operators, out string RegistryJid))
				return (new SearchResultThing[0], RegistryJid);

			SearchResultThing[] Things = await this.SearchAll(RegistryJid, Operators);

			return (Things, RegistryJid);
		}

		/// <summary>
		/// Searches for all devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>Complete list of devices in registry matching the search operators.</returns>
		public async Task<SearchResultThing[]> SearchAll(string RegistryJid, params SearchOperator[] Operators)
		{
			(SearchResultThing[] Things, bool More) = await this.Search(0, Constants.BatchSizes.DeviceBatchSize, RegistryJid, Operators);
			if (!More)
				return Things;

			List<SearchResultThing> Result = new();
			int Offset = Things.Length;

			Result.AddRange(Things);

			while (More)
			{
				(Things, More) = await this.Search(Offset, Constants.BatchSizes.DeviceBatchSize, RegistryJid, Operators);
				Result.AddRange(Things);
				Offset += Things.Length;
			}

			return Result.ToArray();
		}

		#endregion

		#region Legal Identities

		/// <summary>
		/// Adds a legal identity.
		/// </summary>
		/// <param name="Model">The model holding all the values needed.</param>
		/// <param name="Attachments">The physical attachments to upload.</param>
		/// <returns>Legal Identity</returns>
		public async Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel Model, params LegalIdentityAttachment[] Attachments)
		{
			await this.ContractsClient.GenerateNewKeys();

			LegalIdentity identity = await this.ContractsClient.ApplyAsync(Model.ToProperties(this.XmppService));

			foreach (LegalIdentityAttachment a in Attachments)
			{
				HttpFileUploadEventArgs e2 = await this.XmppService.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
				if (!e2.Ok)
					throw e2.StanzaError ?? new Exception(e2.ErrorText);

				await e2.PUT(a.Data, a.ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

				byte[] signature = await this.ContractsClient.SignAsync(a.Data, SignWith.CurrentKeys);

				identity = await this.ContractsClient.AddLegalIdAttachmentAsync(identity.Id, e2.GetUrl, signature);
			}

			await this.ContractsClient.ReadyForApprovalAsync(identity.Id);

			return identity;
		}

		/// <summary>
		/// Returns a list of legal identities.
		/// </summary>
		/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
		/// <returns>Legal Identities</returns>
		public async Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null)
		{
			if (client is null)
				return await this.ContractsClient.GetLegalIdentitiesAsync();
			else
			{
				using ContractsClient cc = new(client, this.TagProfile.LegalJid);  // No need to load keys for this operation.
				return await cc.GetLegalIdentitiesAsync();
			}
		}

		/// <summary>
		/// Gets a specific legal identity.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
		/// <returns>Legal identity object</returns>
		public async Task<LegalIdentity> GetLegalIdentity(CaseInsensitiveString legalIdentityId)
		{
			ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);
			if (Info is not null && Info.LegalIdentity is not null)
				return Info.LegalIdentity;

			return await this.ContractsClient.GetLegalIdentityAsync(legalIdentityId);
		}

		/// <summary>
		/// Checks if a legal identity is in the contacts list.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
		/// <returns>If the legal identity is in the contacts list.</returns>
		public async Task<bool> IsContact(CaseInsensitiveString legalIdentityId)
		{
			ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);
			return (Info is not null && Info.LegalIdentity is not null);
		}

		/// <summary>
		/// Checks if the client has access to the private keys of the specified legal identity.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity.</param>
		/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
		/// <returns>If private keys are available.</returns>
		public async Task<bool> HasPrivateKey(CaseInsensitiveString legalIdentityId, XmppClient client = null)
		{
			if (client is null)
				return await this.ContractsClient.HasPrivateKey(legalIdentityId);
			else
			{
				using ContractsClient cc = new(client, this.TagProfile.LegalJid);

				if (!await cc.LoadKeys(false))
					return false;

				return await cc.HasPrivateKey(legalIdentityId);
			}
		}

		/// <summary>
		/// Marks the legal identity as obsolete.
		/// </summary>
		/// <param name="legalIdentityId">The id to mark as obsolete.</param>
		/// <returns>Legal Identity</returns>
		public Task<LegalIdentity> ObsoleteLegalIdentity(CaseInsensitiveString legalIdentityId)
		{
			return this.ContractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
		}

		/// <summary>
		/// Marks the legal identity as compromised.
		/// </summary>
		/// <param name="legalIdentityId">The legal id to mark as compromised.</param>
		/// <returns>Legal Identity</returns>
		public Task<LegalIdentity> CompromiseLegalIdentity(CaseInsensitiveString legalIdentityId)
		{
			return this.ContractsClient.CompromisedLegalIdentityAsync(legalIdentityId);
		}

		/// <summary>
		/// Petitions a legal identity.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose of the petitioning.</param>
		public async Task PetitionIdentity(CaseInsensitiveString LegalId, string PetitionId, string Purpose)
		{
			await this.ContractsClient.AuthorizeAccessToIdAsync(this.TagProfile.LegalIdentity.Id, LegalId, true);
			await this.ContractsClient.PetitionIdentityAsync(LegalId, PetitionId, Purpose);
		}

		/// <summary>
		/// Sends a response to a petitioning identity request.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
		/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
		public Task SendPetitionIdentityResponse(CaseInsensitiveString LegalId, string PetitionId, string RequestorFullJid, bool Response)
		{
			return this.ContractsClient.PetitionIdentityResponseAsync(LegalId, PetitionId, RequestorFullJid, Response);
		}

		/// <summary>
		/// An event that fires when a legal identity changes.
		/// </summary>
		public event LegalIdentityEventHandler LegalIdentityChanged;

		private async Task ContractsClient_IdentityUpdated(object Sender, LegalIdentityEventArgs e)
		{
			if (this.TagProfile.LegalIdentity is null ||
				this.TagProfile.LegalIdentity.Id == e.Identity.Id ||
				this.TagProfile.LegalIdentity.Created < e.Identity.Created)
			{
				try
				{
					this.LegalIdentityChanged?.Invoke(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
				}
			}
		}

		/// <summary>
		/// An event that fires when a petition for an identity is received.
		/// </summary>
		public event LegalIdentityPetitionEventHandler PetitionForIdentityReceived;

		private async Task ContractsClient_PetitionForIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				this.PetitionForIdentityReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// An event that fires when a petitioned identity response is received.
		/// </summary>
		public event LegalIdentityPetitionResponseEventHandler PetitionedIdentityResponseReceived;

		private async Task ContractsClient_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			try
			{
				this.PetitionedIdentityResponseReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// Exports Keys to XML.
		/// </summary>
		/// <param name="Output">XML output.</param>
		public Task ExportSigningKeys(XmlWriter Output)
		{
			return this.ContractsClient.ExportKeys(Output);
		}

		/// <summary>
		/// Imports keys
		/// </summary>
		/// <param name="Xml">XML Definition of keys.</param>
		/// <returns>If keys could be loaded into the client.</returns>
		public Task<bool> ImportSigningKeys(XmlElement Xml)
		{
			return this.ContractsClient.ImportKeys(Xml);
		}

		/// <summary>
		/// Validates a legal identity.
		/// </summary>
		/// <param name="Identity">Legal Identity</param>
		/// <returns>The validity of the identity.</returns>
		public Task<IdentityStatus> ValidateIdentity(LegalIdentity Identity)
		{
			return this.ContractsClient.ValidateAsync(Identity, true);
		}

		#endregion

		#region Smart Contracts

		private readonly Dictionary<CaseInsensitiveString, DateTime> lastContractEvent = new();

		/// <summary>
		/// Reference to contracts client, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private ContractsClient ContractsClient
		{
			get
			{
				if (this.contractsClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["LegalServiceNotFound"]);

				return this.contractsClient;
			}
		}

		private void RegisterContractsEventHandlers()
		{
			this.ContractsClient.IdentityUpdated += this.ContractsClient_IdentityUpdated;
			this.ContractsClient.PetitionForIdentityReceived += this.ContractsClient_PetitionForIdentityReceived;
			this.ContractsClient.PetitionedIdentityResponseReceived += this.ContractsClient_PetitionedIdentityResponseReceived;
			this.ContractsClient.PetitionForContractReceived += this.ContractsClient_PetitionForContractReceived;
			this.ContractsClient.PetitionedContractResponseReceived += this.ContractsClient_PetitionedContractResponseReceived;
			this.ContractsClient.PetitionForSignatureReceived += this.ContractsClient_PetitionForSignatureReceived;
			this.ContractsClient.PetitionedSignatureResponseReceived += this.ContractsClient_PetitionedSignatureResponseReceived;
			this.ContractsClient.PetitionForPeerReviewIDReceived += this.ContractsClient_PetitionForPeerReviewIdReceived;
			this.ContractsClient.PetitionedPeerReviewIDResponseReceived += this.ContractsClient_PetitionedPeerReviewIdResponseReceived;
			this.ContractsClient.ContractProposalReceived += this.ContractsClient_ContractProposalReceived;
			this.ContractsClient.ContractUpdated += this.ContractsClient_ContractUpdated;
			this.ContractsClient.ContractSigned += this.ContractsClient_ContractSigned;
		}

		/// <summary>
		/// Gets the contract with the specified id.
		/// </summary>
		/// <param name="ContractId">The contract id.</param>
		/// <returns>Smart Contract</returns>
		public Task<Contract> GetContract(CaseInsensitiveString ContractId)
		{
			return this.ContractsClient.GetContractAsync(ContractId);
		}

		/// <summary>
		/// Gets created contracts.
		/// </summary>
		/// <returns>Created contracts.</returns>
		public async Task<string[]> GetCreatedContractReferences()
		{
			List<string> Result = new();
			string[] ContractIds;
			int Offset = 0;
			int Nr;

			do
			{
				ContractIds = await this.ContractsClient.GetCreatedContractReferencesAsync(Offset, 20);
				Result.AddRange(ContractIds);
				Nr = ContractIds.Length;
				Offset += Nr;
			}
			while (Nr == 20);

			return Result.ToArray();
		}

		/// <summary>
		/// Gets signed contracts.
		/// </summary>
		/// <returns>Signed contracts.</returns>
		public async Task<string[]> GetSignedContractReferences()
		{
			List<string> Result = new();
			string[] ContractIds;
			int Offset = 0;
			int Nr;

			do
			{
				ContractIds = await this.ContractsClient.GetSignedContractReferencesAsync(Offset, 20);
				Result.AddRange(ContractIds);
				Nr = ContractIds.Length;
				Offset += Nr;
			}
			while (Nr == 20);

			return Result.ToArray();
		}

		/// <summary>
		/// Signs a given contract.
		/// </summary>
		/// <param name="Contract">The contract to sign.</param>
		/// <param name="Role">The role of the signer.</param>
		/// <param name="Transferable">Whether the contract is transferable or not.</param>
		/// <returns>Smart Contract</returns>
		public async Task<Contract> SignContract(Contract Contract, string Role, bool Transferable)
		{
			Contract Result = await this.ContractsClient.SignContractAsync(Contract, Role, Transferable);
			await this.UpdateContractReference(Result);
			return Result;
		}

		/// <summary>
		/// Obsoletes a contract.
		/// </summary>
		/// <param name="ContractId">The id of the contract to obsolete.</param>
		/// <returns>Smart Contract</returns>
		public async Task<Contract> ObsoleteContract(CaseInsensitiveString ContractId)
		{
			Contract Result = await this.ContractsClient.ObsoleteContractAsync(ContractId);
			await this.UpdateContractReference(Result);
			return Result;
		}

		/// <summary>
		/// Creates a new contract.
		/// </summary>
		/// <param name="TemplateId">The id of the contract template to use.</param>
		/// <param name="Parts">The individual contract parts.</param>
		/// <param name="Parameters">Contract parameters.</param>
		/// <param name="Visibility">The contract's visibility.</param>
		/// <param name="PartsMode">The contract's parts.</param>
		/// <param name="Duration">Duration of the contract.</param>
		/// <param name="ArchiveRequired">Required duration for contract archival.</param>
		/// <param name="ArchiveOptional">Optional duration for contract archival.</param>
		/// <param name="SignAfter">Timestamp of when the contract can be signed at the earliest.</param>
		/// <param name="SignBefore">Timestamp of when the contract can be signed at the latest.</param>
		/// <param name="CanActAsTemplate">Can this contract act as a template itself?</param>
		/// <returns>Smart Contract</returns>
		public async Task<Contract> CreateContract(
			CaseInsensitiveString TemplateId,
			Part[] Parts,
			Parameter[] Parameters,
			ContractVisibility Visibility,
			ContractParts PartsMode,
			Duration Duration,
			Duration ArchiveRequired,
			Duration ArchiveOptional,
			DateTime? SignAfter,
			DateTime? SignBefore,
			bool CanActAsTemplate)
		{
			Contract Result = await this.ContractsClient.CreateContractAsync(TemplateId, Parts, Parameters, Visibility, PartsMode, Duration, ArchiveRequired, ArchiveOptional, SignAfter, SignBefore, CanActAsTemplate);
			await this.UpdateContractReference(Result);
			return Result;
		}

		/// <summary>
		/// Deletes a contract.
		/// </summary>
		/// <param name="ContractId">The id of the contract to delete.</param>
		/// <returns>Smart Contract</returns>
		public async Task<Contract> DeleteContract(CaseInsensitiveString ContractId)
		{
			Contract Contract = await this.ContractsClient.DeleteContractAsync(ContractId);
			await this.UpdateContractReference(Contract);
			return Contract;
		}

		/// <summary>
		/// Petitions a contract with the specified id and purpose.
		/// </summary>
		/// <param name="ContractId">The contract id.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose.</param>
		public Task PetitionContract(CaseInsensitiveString ContractId, string PetitionId, string Purpose)
		{
			return this.ContractsClient.PetitionContractAsync(ContractId, PetitionId, Purpose);
		}

		/// <summary>
		/// Sends a response to a petitioning contract request.
		/// </summary>
		/// <param name="ContractId">The id of the contract.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
		/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
		public Task SendPetitionContractResponse(CaseInsensitiveString ContractId, string PetitionId, string RequestorFullJid, bool Response)
		{
			return this.ContractsClient.PetitionContractResponseAsync(ContractId, PetitionId, RequestorFullJid, Response);
		}

		/// <summary>
		/// An event that fires when a petition for a contract is received.
		/// </summary>
		public event ContractPetitionEventHandler PetitionForContractReceived;

		private async Task ContractsClient_PetitionForContractReceived(object Sender, ContractPetitionEventArgs e)
		{
			try
			{
				this.PetitionForContractReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// An event that fires when a petitioned contract response is received.
		/// </summary>
		public event ContractPetitionResponseEventHandler PetitionedContractResponseReceived;

		private async Task ContractsClient_PetitionedContractResponseReceived(object Sender, ContractPetitionResponseEventArgs e)
		{
			try
			{
				this.PetitionedContractResponseReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// Gets the timestamp of the last event received for a given contract ID.
		/// </summary>
		/// <param name="ContractId">Contract ID</param>
		/// <returns>Timestamp</returns>
		public DateTime GetTimeOfLastContractEvent(CaseInsensitiveString ContractId)
		{
			lock (this.lastContractEvent)
			{
				if (this.lastContractEvent.TryGetValue(ContractId, out DateTime TP))
					return TP;
				else
					return DateTime.MinValue;
			}
		}

		private async Task UpdateContractReference(Contract Contract)
		{
			ContractReference Ref = await Database.FindFirstDeleteRest<ContractReference>(
				new FilterFieldEqualTo("ContractId", Contract.ContractId));

			if (Ref is null)
			{
				Ref = new ContractReference()
				{
					ContractId = Contract.ContractId
				};

				await Ref.SetContract(Contract, this);
				await Database.Insert(Ref);
			}
			else
			{
				await Ref.SetContract(Contract, this);
				await Database.Update(Ref);
			}
		}

		/// <summary>
		/// Event raised when a contract proposal has been received.
		/// </summary>
		public event ContractProposalEventHandler ContractProposalReceived;

		private async Task ContractsClient_ContractProposalReceived(object Sender, ContractProposalEventArgs e)
		{
			try
			{
				this.ContractProposalReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// Event raised when contract was updated.
		/// </summary>
		public event ContractReferenceEventHandler ContractUpdated;

		private async Task ContractsClient_ContractUpdated(object Sender, ContractReferenceEventArgs e)
		{
			await this.ContractUpdatedOrSigned(e);

			try
			{
				this.ContractUpdated?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService?.LogException(ex);
			}
		}

		private Task ContractUpdatedOrSigned(ContractReferenceEventArgs e)
		{
			lock (this.lastContractEvent)
			{
				this.lastContractEvent[e.ContractId] = DateTime.Now;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Event raised when contract was signed.
		/// </summary>
		public event ContractSignedEventHandler ContractSigned;

		private async Task ContractsClient_ContractSigned(object Sender, ContractSignedEventArgs e)
		{
			await this.ContractUpdatedOrSigned(e);

			try
			{
				this.ContractSigned?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService?.LogException(ex);
			}
		}

		/// <summary>
		/// Sends a contract proposal to a recipient.
		/// </summary>
		/// <param name="ContractId">ID of proposed contract.</param>
		/// <param name="Role">Proposed role of recipient.</param>
		/// <param name="To">Recipient Address (Bare or Full JID).</param>
		/// <param name="Message">Optional message included in message.</param>
		public void SendContractProposal(string ContractId, string Role, string To, string Message)
		{
			this.ContractsClient.SendContractProposal(ContractId, Role, To, Message);
		}

		#endregion

		#region Attachments

		/// <summary>
		/// Gets an attachment for a contract.
		/// </summary>
		/// <param name="Url">The url of the attachment.</param>
		/// <param name="Timeout">Max timeout allowed when retrieving an attachment.</param>
		/// <param name="SignWith">How the request is signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
		/// <returns>Content-Type, and attachment file.</returns>
		public Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string Url, SignWith SignWith, TimeSpan Timeout)
		{
			return this.ContractsClient.GetAttachmentAsync(Url, SignWith, (int)Timeout.TotalMilliseconds);
		}

		#endregion

		#region Peer Review

		/// <summary>
		/// Sends a petition to a third-party to review a legal identity.
		/// </summary>
		/// <param name="LegalId">The legal id to petition.</param>
		/// <param name="Identity">The legal id to peer review.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose.</param>
		public async Task PetitionPeerReviewId(CaseInsensitiveString LegalId, LegalIdentity Identity, string PetitionId, string Purpose)
		{
			await this.ContractsClient.AuthorizeAccessToIdAsync(Identity.Id, LegalId, true);
			await this.ContractsClient.PetitionPeerReviewIDAsync(LegalId, Identity, PetitionId, Purpose);
		}

		/// <summary>
		/// Adds an attachment for the peer review.
		/// </summary>
		/// <param name="Identity">The identity to which the attachment should be added.</param>
		/// <param name="ReviewerLegalIdentity">The identity of the reviewer.</param>
		/// <param name="PeerSignature">The raw signature data.</param>
		/// <returns>Legal Identity</returns>
		public Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity Identity, LegalIdentity ReviewerLegalIdentity, byte[] PeerSignature)
		{
			return this.ContractsClient.AddPeerReviewIDAttachment(Identity, ReviewerLegalIdentity, PeerSignature);
		}

		/// <summary>
		/// An event that fires when a petition for peer review is received.
		/// </summary>
		public event SignaturePetitionEventHandler PetitionForPeerReviewIdReceived;

		private async Task ContractsClient_PetitionForPeerReviewIdReceived(object Sender, SignaturePetitionEventArgs e)
		{
			try
			{
				this.PetitionForPeerReviewIdReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// An event that fires when a petitioned peer review response is received.
		/// </summary>
		public event SignaturePetitionResponseEventHandler PetitionedPeerReviewIdResponseReceived;

		private async Task ContractsClient_PetitionedPeerReviewIdResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				this.PetitionedPeerReviewIdResponseReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		#endregion

		#region Signatures

		/// <summary>
		/// Signs binary data with the corresponding private key.
		/// </summary>
		/// <param name="data">The data to sign.</param>
		/// <param name="signWith">What keys that can be used to sign the data.</param>
		/// <returns>Signature</returns>
		public Task<byte[]> Sign(byte[] data, SignWith signWith)
		{
			return this.ContractsClient.SignAsync(data, signWith);
		}

		/// <summary>Validates a signature of binary data.</summary>
		/// <param name="legalIdentity">Legal identity used to create the signature.</param>
		/// <param name="data">Binary data to sign-</param>
		/// <param name="signature">Digital signature of data</param>
		/// <returns>
		/// true = Signature is valid.
		/// false = Signature is invalid.
		/// null = Client key algorithm is unknown, and veracity of signature could not be established.
		/// </returns>
		public bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature)
		{
			return this.ContractsClient.ValidateSignature(legalIdentity, data, signature);
		}

		/// <summary>
		/// Sends a response to a petitioning signature request.
		/// </summary>
		/// <param name="LegalId">Legal Identity petitioned.</param>
		/// <param name="Content">Content to be signed.</param>
		/// <param name="Signature">Digital signature of content, made by the legal identity.</param>
		/// <param name="PetitionId">A petition identifier. This identifier will follow the petition, and can be used
		/// to identify the petition request.</param>
		/// <param name="RequestorFullJid">Full JID of requestor.</param>
		/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
		public Task SendPetitionSignatureResponse(CaseInsensitiveString LegalId, byte[] Content, byte[] Signature, string PetitionId, string RequestorFullJid, bool Response)
		{
			return this.ContractsClient.PetitionSignatureResponseAsync(LegalId, Content, Signature, PetitionId, RequestorFullJid, Response);
		}

		/// <summary>
		/// An event that fires when a petition for a signature is received.
		/// </summary>
		public event SignaturePetitionEventHandler PetitionForSignatureReceived;

		private async Task ContractsClient_PetitionForSignatureReceived(object Sender, SignaturePetitionEventArgs e)
		{
			try
			{
				this.PetitionForSignatureReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		/// <summary>
		/// Event raised when a response to a signature petition has been received.
		/// </summary>
		public event SignaturePetitionResponseEventHandler SignaturePetitionResponseReceived;

		private async Task ContractsClient_PetitionedSignatureResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				this.SignaturePetitionResponseReceived?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
			}
		}

		#endregion

		#region Provisioning

		/// <summary>
		/// Access to provisioning client, for authorization control, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private ProvisioningClient ProvisioningClient
		{
			get
			{
				if (this.provisioningClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["ProvisioningServiceNotFound"]);

				return this.provisioningClient;
			}
		}

		private async Task ProvisioningClient_IsFriendQuestion(object Sender, IsFriendEventArgs e)
		{
			if (e.From.IndexOfAny(clientChars) < 0)
				await this.NotificationService.NewEvent(new IsFriendNotificationEvent(e));
		}

		private async Task ProvisioningClient_CanReadQuestion(object Sender, CanReadEventArgs e)
		{
			if (e.From.IndexOfAny(clientChars) < 0)
				await this.NotificationService.NewEvent(new CanReadNotificationEvent(e));
		}

		private async Task ProvisioningClient_CanControlQuestion(object Sender, CanControlEventArgs e)
		{
			if (e.From.IndexOfAny(clientChars) < 0)
				await this.NotificationService.NewEvent(new CanControlNotificationEvent(e));
		}

		private readonly static char[] clientChars = new char[] { '@', '/' };

		/// <summary>
		/// JID of provisioning service.
		/// </summary>
		public string ProvisioningServiceJid => this.ProvisioningClient.ProvisioningServerAddress;

		/// <summary>
		/// Sends a response to a previous "Is Friend" question.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="IsFriend">If the response is yes or no.</param>
		/// <param name="Range">The range of the response.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void IsFriendResponse(string ProvisioningServiceJID, string JID, string RemoteJID, string Key, bool IsFriend,
			RuleRange Range, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.IsFriendResponse(ProvisioningServiceJID, JID, RemoteJID, Key, IsFriend, Range, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, for all future requests.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseAll(string ProvisioningServiceJID, string JID, string RemoteJID, string Key, bool CanControl,
			string[] ParameterNames, IThingReference Node, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.CanControlResponseAll(ProvisioningServiceJID, JID, RemoteJID, Key, CanControl, ParameterNames,
				Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on the JID of the caller.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseCaller(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanControl, string[] ParameterNames, IThingReference Node, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.CanControlResponseCaller(ProvisioningServiceJID, JID, RemoteJID, Key, CanControl,
				ParameterNames, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on the domain of the caller.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseDomain(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanControl, string[] ParameterNames, IThingReference Node, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.CanControlResponseDomain(ProvisioningServiceJID, JID, RemoteJID, Key, CanControl,
				ParameterNames, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on a device token.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseDevice(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanControl, string[] ParameterNames, string Token, IThingReference Node, IqResultEventHandlerAsync Callback,
			object State)
		{
			this.ProvisioningClient.CanControlResponseDevice(ProvisioningServiceJID, JID, RemoteJID, Key, CanControl,
				ParameterNames, Token, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on a service token.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseService(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanControl, string[] ParameterNames, string Token, IThingReference Node, IqResultEventHandlerAsync Callback,
			object State)
		{
			this.ProvisioningClient.CanControlResponseService(ProvisioningServiceJID, JID, RemoteJID, Key, CanControl,
				ParameterNames, Token, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on a user token.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseUser(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanControl, string[] ParameterNames, string Token, IThingReference Node, IqResultEventHandlerAsync Callback,
			object State)
		{
			this.ProvisioningClient.CanControlResponseUser(ProvisioningServiceJID, JID, RemoteJID, Key, CanControl,
				ParameterNames, Token, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, for all future requests.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to control the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseAll(string ProvisioningServiceJID, string JID, string RemoteJID, string Key, bool CanRead,
			FieldType FieldTypes, string[] FieldNames, IThingReference Node, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.CanReadResponseAll(ProvisioningServiceJID, JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames,
				Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on the JID of the caller.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to control the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseCaller(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanRead, FieldType FieldTypes, string[] FieldNames, IThingReference Node, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.CanReadResponseCaller(ProvisioningServiceJID, JID, RemoteJID, Key, CanRead,
				FieldTypes, FieldNames, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on the domain of the caller.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to control the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseDomain(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanRead, FieldType FieldTypes, string[] FieldNames, IThingReference Node, IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.CanReadResponseDomain(ProvisioningServiceJID, JID, RemoteJID, Key, CanRead,
				FieldTypes, FieldNames, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on a device token.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to control the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseDevice(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanRead, FieldType FieldTypes, string[] FieldNames, string Token, IThingReference Node, IqResultEventHandlerAsync Callback,
			object State)
		{
			this.ProvisioningClient.CanReadResponseDevice(ProvisioningServiceJID, JID, RemoteJID, Key, CanRead,
				FieldTypes, FieldNames, Token, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on a service token.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to control the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseService(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanRead, FieldType FieldTypes, string[] FieldNames, string Token, IThingReference Node, IqResultEventHandlerAsync Callback,
			object State)
		{
			this.ProvisioningClient.CanReadResponseService(ProvisioningServiceJID, JID, RemoteJID, Key, CanRead,
				FieldTypes, FieldNames, Token, Node, Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on a user token.
		/// </summary>
		/// <param name="ProvisioningServiceJID">Provisioning service JID.</param>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to control the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or Waher.Things.ThingReference.Empty.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseUser(string ProvisioningServiceJID, string JID, string RemoteJID, string Key,
			bool CanRead, FieldType FieldTypes, string[] FieldNames, string Token, IThingReference Node, IqResultEventHandlerAsync Callback,
			object State)
		{
			this.ProvisioningClient.CanReadResponseUser(ProvisioningServiceJID, JID, RemoteJID, Key, CanRead,
				FieldTypes, FieldNames, Token, Node, Callback, State);
		}

		/// <summary>
		/// Deletes the rules of a device.
		/// </summary>
		/// <param name="ServiceJID">JID of provisioning service.</param>
		/// <param name="DeviceJID">Bare JID of device whose rules are to be deleted. If null, all owned devices will get their rules deleted.</param>
		/// <param name="NodeId">Optional Node ID of device.</param>
		/// <param name="SourceId">Optional Source ID of device.</param>
		/// <param name="Partition">Optional Partition of device.</param>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void DeleteDeviceRules(string ServiceJID, string DeviceJID, string NodeId, string SourceId, string Partition,
			IqResultEventHandlerAsync Callback, object State)
		{
			this.ProvisioningClient.DeleteDeviceRules(ServiceJID, DeviceJID, NodeId, SourceId, Partition, Callback, State);
		}

		#endregion

		#region IoT

		/// <summary>
		/// Access to sensor client, for sensor data readout and subscription, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private SensorClient SensorClient
		{
			get
			{
				if (this.sensorClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["SensorServiceNotFound"]);

				return this.sensorClient;
			}
		}

		/// <summary>
		/// Access to control client, for access to actuators, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private ControlClient ControlClient
		{
			get
			{
				if (this.controlClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["ControlServiceNotFound"]);

				return this.controlClient;
			}
		}

		/// <summary>
		/// Access to concentrator client, for administrative purposes of concentrators, with a check that one is created.
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private ConcentratorClient ConcentratorClient
		{
			get
			{
				if (this.concentratorClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["ConcentratorServiceNotFound"]);

				return this.concentratorClient;
			}
		}

		/// <summary>
		/// Gets a (partial) list of my devices.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <returns>Found devices, and if there are more devices available.</returns>
		public Task<(SearchResultThing[], bool)> GetMyDevices(int Offset, int MaxCount)
		{
			TaskCompletionSource<(SearchResultThing[], bool)> Result = new();

			this.ProvisioningClient.GetDevices(Offset, MaxCount, (sender, e) =>
			{
				if (e.Ok)
					Result.TrySetResult((e.Things, e.More));
				else
					Result.TrySetException(e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetListOfMyDevices"]));

				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Gets the full list of my devices.
		/// </summary>
		/// <returns>Complete list of my devices.</returns>
		public async Task<SearchResultThing[]> GetAllMyDevices()
		{
			(SearchResultThing[] Things, bool More) = await this.GetMyDevices(0, Constants.BatchSizes.DeviceBatchSize);
			if (!More)
				return Things;

			List<SearchResultThing> Result = new();
			int Offset = Things.Length;

			Result.AddRange(Things);

			while (More)
			{
				(Things, More) = await this.GetMyDevices(Offset, Constants.BatchSizes.DeviceBatchSize);
				Result.AddRange(Things);
				Offset += Things.Length;
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Gets the certificate the corresponds to a token. This certificate can be used
		/// to identify services, devices or users. Tokens are challenged to make sure they
		/// correspond to the holder of the private part of the corresponding certificate.
		/// </summary>
		/// <param name="Token">Token corresponding to the requested certificate.</param>
		/// <param name="Callback">Callback method called, when certificate is available.</param>
		/// <param name="State">State object that will be passed on to the callback method.</param>
		public void GetCertificate(string Token, CertificateCallback Callback, object State)
		{
			this.ProvisioningClient.GetCertificate(Token, Callback, State);
		}

		/// <summary>
		/// Gets a control form from an actuator.
		/// </summary>
		/// <param name="To">Address of actuator.</param>
		/// <param name="Language">Language</param>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object.</param>
		/// <param name="Nodes">Node references</param>
		public void GetControlForm(string To, string Language, DataFormResultEventHandler Callback, object State,
			params ThingReference[] Nodes)
		{
			this.ControlClient.GetForm(To, Language, Callback, State, Nodes);
		}

		/// <summary>
		/// Requests a sensor data readout.
		/// </summary>
		/// <param name="Destination">JID of sensor to read.</param>
		/// <param name="Types">Field Types to read.</param>
		/// <returns>Request object maintaining the current status of the request.</returns>
		public SensorDataClientRequest RequestSensorReadout(string Destination, FieldType Types)
		{
			return this.SensorClient.RequestReadout(Destination, Types);
		}

		/// <summary>
		/// Requests a sensor data readout.
		/// </summary>
		/// <param name="Destination">JID of sensor to read.</param>
		/// <param name="Nodes">Array of nodes to read. Can be null or empty, if reading a sensor that is not a concentrator.</param>
		/// <param name="Types">Field Types to read.</param>
		/// <returns>Request object maintaining the current status of the request.</returns>
		public SensorDataClientRequest RequestSensorReadout(string Destination, ThingReference[] Nodes, FieldType Types)
		{
			return this.SensorClient.RequestReadout(Destination, Nodes, Types);
		}

		#endregion

		#region e-Daler

		private readonly Dictionary<string, PaymentTransaction> currentTransactions = new();
		private Balance lastBalance = null;
		private DateTime lastEDalerEvent = DateTime.MinValue;

		/// <summary>
		/// Reference to the e-Daler client implementing the e-Daler XMPP extension
		/// Note: Do not make public. Reference only from inside the XmppService class.
		/// </summary>
		private EDalerClient EDalerClient
		{
			get
			{
				if (this.eDalerClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["EDalerServiceNotFound"]);

				return this.eDalerClient;
			}
		}

		private void RegisterEDalerEventHandlers(EDalerClient Client)
		{
			Client.BalanceUpdated += this.EDalerClient_BalanceUpdated;
			Client.ClientUrlReceived += this.NeuroWallet_ClientUrlReceived;
			Client.PaymentCompleted += this.NeuroWallet_PaymentCompleted;
			Client.PaymentError += this.NeuroWallet_PaymentError;
		}

		private async Task EDalerClient_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			this.lastBalance = e.Balance;
			this.lastEDalerEvent = DateTime.Now;

			BalanceEventHandler h = this.EDalerBalanceUpdated;
			if (h is not null)
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when balance has been updated
		/// </summary>
		public event BalanceEventHandler EDalerBalanceUpdated;

		/// <summary>
		/// Last reported balance
		/// </summary>
		public Balance LastEDalerBalance => this.lastBalance;

		/// <summary>
		/// Timepoint of last event.
		/// </summary>
		public DateTime LastEDalerEvent => this.lastEDalerEvent;

		/// <summary>
		/// Tries to parse an eDaler URI.
		/// </summary>
		/// <param name="Uri">URI string.</param>
		/// <param name="Parsed">Parsed eDaler URI, if successful.</param>
		/// <param name="Reason">Error message, if not able to parse URI.</param>
		/// <returns>If URI string could be parsed.</returns>
		public bool TryParseEDalerUri(string Uri, out EDalerUri Parsed, out string Reason)
		{
			return EDalerUri.TryParse(Uri, out Parsed, out Reason);
		}

		/// <summary>
		/// Tries to decrypt an encrypted private message.
		/// </summary>
		/// <param name="EncryptedMessage">Encrypted message.</param>
		/// <param name="PublicKey">Public key used.</param>
		/// <param name="TransactionId">ID of transaction containing the encrypted message.</param>
		/// <param name="RemoteEndpoint">Remote endpoint</param>
		/// <returns>Decrypted string, if successful, or null, if not.</returns>
		public async Task<string> TryDecryptMessage(byte[] EncryptedMessage, byte[] PublicKey, Guid TransactionId, string RemoteEndpoint)
		{
			try
			{
				return await this.EDalerClient.DecryptMessage(EncryptedMessage, PublicKey, TransactionId, RemoteEndpoint);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				return string.Empty;
			}
		}

		/// <summary>
		/// Sends an eDaler URI to the eDaler service.
		/// </summary>
		/// <param name="Uri">eDaler URI</param>
		/// <returns>Transaction object containing information about the processed URI.</returns>
		public Task<Transaction> SendEDalerUri(string Uri)
		{
			return this.EDalerClient.SendEDalerUriAsync(Uri);
		}

		/// <summary>
		/// Gets account events available for the wallet.
		/// </summary>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <returns>Events found, and if more events are available.</returns>
		public Task<(AccountEvent[], bool)> GetEDalerAccountEvents(int MaxCount)
		{
			return this.EDalerClient.GetAccountEventsAsync(MaxCount);
		}

		/// <summary>
		/// Gets account events available for the wallet.
		/// </summary>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <param name="From">From what point in time events should be returned.</param>
		/// <returns>Events found, and if more events are available.</returns>
		public Task<(AccountEvent[], bool)> GetEDalerAccountEvents(int MaxCount, DateTime From)
		{
			return this.EDalerClient.GetAccountEventsAsync(MaxCount, From);
		}

		/// <summary>
		/// Gets the current account balance.
		/// </summary>
		/// <returns>Current account balance.</returns>
		public Task<Balance> GetEDalerBalance()
		{
			return this.EDalerClient.GetBalanceAsync();
		}

		/// <summary>
		/// Gets pending payments
		/// </summary>
		/// <returns>(Total amount, currency, items)</returns>
		public Task<(decimal, string, PendingPayment[])> GetPendingEDalerPayments()
		{
			return this.EDalerClient.GetPendingPayments();
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="ToBareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullEDalerPaymentUri(string ToBareJid, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays)
		{
			this.lastEDalerEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(ToBareJid, Amount, AmountExtra, Currency, ValidNrDays);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="ToBareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <param name="Message">Unencrypted message to send to recipient.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullEDalerPaymentUri(string ToBareJid, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays, string Message)
		{
			this.lastEDalerEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(ToBareJid, Amount, AmountExtra, Currency, ValidNrDays, Message);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullEDalerPaymentUri(LegalIdentity To, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays)
		{
			this.lastEDalerEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(To, Amount, AmountExtra, Currency, ValidNrDays);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <param name="PrivateMessage">Message to be sent to recipient. Message will be end-to-end encrypted.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullEDalerPaymentUri(LegalIdentity To, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays, string PrivateMessage)
		{
			this.lastEDalerEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(To, Amount, AmountExtra, Currency, ValidNrDays, PrivateMessage);
		}

		/// <summary>
		/// Creates an incomplete PayMe-URI.
		/// </summary>
		/// <param name="BareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="Message">Message to be sent to recipient (not encrypted).</param>
		/// <returns>Incomplete PayMe-URI.</returns>
		public string CreateIncompleteEDalerPayMeUri(string BareJid, decimal? Amount, decimal? AmountExtra, string Currency, string Message)
		{
			return this.EDalerClient.CreateIncompletePayMeUri(BareJid, Amount, AmountExtra, Currency, Message);
		}

		/// <summary>
		/// Creates an incomplete PayMe-URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="PrivateMessage">Message to be sent to recipient. Message will be end-to-end encrypted in payment.
		/// But the message will be unencrypted in the incomplete PeyMe URI.</param>
		/// <returns>Incomplete PayMe-URI.</returns>
		public string CreateIncompleteEDalerPayMeUri(LegalIdentity To, decimal? Amount, decimal? AmountExtra, string Currency, string PrivateMessage)
		{
			return this.EDalerClient.CreateIncompletePayMeUri(To, Amount, AmountExtra, Currency, PrivateMessage);
		}

		/// <summary>
		/// Gets available service providers for buying eDaler.
		/// </summary>
		/// <returns>Available service providers.</returns>
		public async Task<IBuyEDalerServiceProvider[]> GetServiceProvidersForBuyingEDalerAsync()
		{
			return await this.EDalerClient.GetServiceProvidersForBuyingEDalerAsync();
		}

		/// <summary>
		/// Initiates payment of eDaler using a service provider that is not based on a smart contract.
		/// </summary>
		/// <param name="ServiceId">Service ID</param>
		/// <param name="ServiceProvider">Service Provider</param>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		/// <returns>Transaction ID</returns>
		public async Task<PaymentTransaction> InitiateEDalerPayment(string ServiceId, string ServiceProvider, decimal Amount, string Currency)
		{
			string TransactionId = Guid.NewGuid().ToString();
			string SuccessUrl = this.GenerateTagIdUrl(
				new KeyValuePair<string, object>("cmd", "ps"),
				new KeyValuePair<string, object>("tid", TransactionId),
				new KeyValuePair<string, object>("amt", Amount),
				new KeyValuePair<string, object>("cur", Currency),
				new KeyValuePair<string, object>(JwtClaims.ClientId, this.CryptoService.DeviceID),
				new KeyValuePair<string, object>(JwtClaims.Issuer, this.CryptoService.DeviceID),
				new KeyValuePair<string, object>(JwtClaims.Subject, this.XmppService.BareJid),
				new KeyValuePair<string, object>(JwtClaims.ExpirationTime, (int)DateTime.UtcNow.AddHours(1).Subtract(JSON.UnixEpoch).TotalSeconds));
			string FailureUrl = this.GenerateTagIdUrl(
				new KeyValuePair<string, object>("cmd", "pf"),
				new KeyValuePair<string, object>("tid", TransactionId),
				new KeyValuePair<string, object>(JwtClaims.ClientId, this.CryptoService.DeviceID),
				new KeyValuePair<string, object>(JwtClaims.Issuer, this.CryptoService.DeviceID),
				new KeyValuePair<string, object>(JwtClaims.Subject, this.XmppService.BareJid),
				new KeyValuePair<string, object>(JwtClaims.ExpirationTime, (int)DateTime.UtcNow.AddHours(1).Subtract(JSON.UnixEpoch).TotalSeconds));
			string CancelUrl = this.GenerateTagIdUrl(
				new KeyValuePair<string, object>("cmd", "pc"),
				new KeyValuePair<string, object>("tid", TransactionId),
				new KeyValuePair<string, object>(JwtClaims.ClientId, this.CryptoService.DeviceID),
				new KeyValuePair<string, object>(JwtClaims.Issuer, this.CryptoService.DeviceID),
				new KeyValuePair<string, object>(JwtClaims.Subject, this.XmppService.BareJid),
				new KeyValuePair<string, object>(JwtClaims.ExpirationTime, (int)DateTime.UtcNow.AddHours(1).Subtract(JSON.UnixEpoch).TotalSeconds));

			TransactionId = await this.EDalerClient.InitiatePaymentOfEDalerAsync(ServiceId, ServiceProvider, Amount, Currency, TransactionId, SuccessUrl, FailureUrl, CancelUrl);
			PaymentTransaction Result = new(TransactionId, Currency);

			lock (this.currentTransactions)
			{
				this.currentTransactions[TransactionId] = Result;
			}

			return Result;
		}

		private string GenerateTagIdUrl(params KeyValuePair<string, object>[] Claims)
		{
			string Token = this.CryptoService.GenerateJwtToken(Claims);
			return Constants.UriSchemes.UriSchemeTagIdApp + ":" + Token;
		}

		private async Task NeuroWallet_ClientUrlReceived(object Sender, ClientUrlEventArgs e)
		{
			PaymentTransaction Transaction;

			lock (this.currentTransactions)
			{
				if (!this.currentTransactions.TryGetValue(e.TransactionId, out Transaction))
				{
					this.LogService.LogWarning("Client URL message ignored. Transaction ID not recognized.",
						new KeyValuePair<string, object>("TransactionId", e.TransactionId),
						new KeyValuePair<string, object>("ClientUrl", e.ClientUrl));
					return;
				}
			}

			await Transaction.OpenUrl(e.ClientUrl);
		}

		private Task NeuroWallet_PaymentError(object Sender, PaymentErrorEventArgs e)
		{
			this.EDalerPaymentFailed(e.TransactionId, e.Message);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Registers an initiated payment as failed.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <param name="Message">Error message.</param>
		public void EDalerPaymentFailed(string TransactionId, string Message)
		{
			PaymentTransaction Transaction;

			lock (this.currentTransactions)
			{
				if (!this.currentTransactions.TryGetValue(TransactionId, out Transaction))
					return;

				this.currentTransactions.Remove(TransactionId);
			}

			Transaction.ErrorReported(Message);
		}

		private Task NeuroWallet_PaymentCompleted(object Sender, PaymentCompletedEventArgs e)
		{
			this.EDalerPaymentCompleted(e.TransactionId, e.Amount, e.Currency);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Registers an initiated payment as completed.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		public void EDalerPaymentCompleted(string TransactionId, decimal Amount, string Currency)
		{
			PaymentTransaction Transaction;

			lock (this.currentTransactions)
			{
				if (!this.currentTransactions.TryGetValue(TransactionId, out Transaction))
					return;

				this.currentTransactions.Remove(TransactionId);
			}

			Transaction.Completed(Amount, Currency);
		}

		#endregion

		#region Neuro-Features

		private DateTime lastTokenEvent = DateTime.MinValue;

		/// <summary>
		/// Reference to the Neuro-Features client implementing the Neuro-Features XMPP extension
		/// </summary>
		private NeuroFeaturesClient NeuroFeaturesClient
		{
			get
			{
				if (this.neuroFeaturesClient is null)
					throw new InvalidOperationException(LocalizationResourceManager.Current["NeuroFeaturesServiceNotFound"]);

				return this.neuroFeaturesClient;
			}
		}

		private void RegisterNeuroFeatureEventHandlers(NeuroFeaturesClient Client)
		{
			Client.TokenAdded += this.NeuroFeaturesClient_TokenAdded;
			Client.TokenRemoved += this.NeuroFeaturesClient_TokenRemoved;

			Client.StateUpdated += this.NeuroFeaturesClient_StateUpdated;
			Client.VariablesUpdated += this.NeuroFeaturesClient_VariablesUpdated;
		}

		/// <summary>
		/// Timepoint of last event.
		/// </summary>
		public DateTime LastNeuroFeatureEvent => this.lastTokenEvent;

		private async Task NeuroFeaturesClient_TokenRemoved(object _, NeuroFeatures.TokenEventArgs e)
		{
			this.lastTokenEvent = DateTime.Now;

			NeuroFeatures.TokenEventHandler h = this.NeuroFeatureRemoved;
			if (h is not null)
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a token has been removed from the wallet.
		/// </summary>
		public event NeuroFeatures.TokenEventHandler NeuroFeatureRemoved;

		private async Task NeuroFeaturesClient_TokenAdded(object _, NeuroFeatures.TokenEventArgs e)
		{
			this.lastTokenEvent = DateTime.Now;

			NeuroFeatures.TokenEventHandler h = this.NeuroFeatureAdded;
			if (h is not null)
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a token has been added to the wallet.
		/// </summary>
		public event NeuroFeatures.TokenEventHandler NeuroFeatureAdded;

		private async Task NeuroFeaturesClient_VariablesUpdated(object Sender, VariablesUpdatedEventArgs e)
		{
			VariablesUpdatedEventHandler h = this.NeuroFeatureVariablesUpdated;
			if (h is not null)
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when variables have been updated in a state-machine.
		/// </summary>
		public event VariablesUpdatedEventHandler NeuroFeatureVariablesUpdated;

		private async Task NeuroFeaturesClient_StateUpdated(object Sender, NewStateEventArgs e)
		{
			NewStateEventHandler h = this.NeuroFeatureStateUpdated;
			if (h is not null)
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a state-machine has received a new state.
		/// </summary>
		public event NewStateEventHandler NeuroFeatureStateUpdated;

		/// <summary>
		/// Gets available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<TokensEventArgs> GetNeuroFeatures()
		{
			return this.GetNeuroFeatures(0, int.MaxValue);
		}

		/// <summary>
		/// Gets a section of available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<TokensEventArgs> GetNeuroFeatures(int Offset, int MaxCount)
		{
			return this.NeuroFeaturesClient.GetTokensAsync(Offset, MaxCount);
		}

		/// <summary>
		/// Gets references to available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<string[]> GetNeuroFeatureReferences()
		{
			return this.GetNeuroFeatureReferences(0, int.MaxValue);
		}

		/// <summary>
		/// Gets references to a section of available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<string[]> GetNeuroFeatureReferences(int Offset, int MaxCount)
		{
			return this.NeuroFeaturesClient.GetTokenReferencesAsync(Offset, MaxCount);
		}

		/// <summary>
		/// Gets the value totals of tokens available in the wallet, grouped and ordered by currency.
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<TokenTotalsEventArgs> GetNeuroFeatureTotals()
		{
			return this.NeuroFeaturesClient.GetTotalsAsync();
		}

		/// <summary>
		/// Gets a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Token</returns>
		public Task<Token> GetNeuroFeature(string TokenId)
		{
			return this.NeuroFeaturesClient.GetTokenAsync(TokenId);
		}

		/// <summary>
		/// Gets events relating to a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Token events.</returns>
		public Task<TokenEvent[]> GetNeuroFeatureEvents(string TokenId)
		{
			return this.GetNeuroFeatureEvents(TokenId, 0, int.MaxValue);
		}

		/// <summary>
		/// Gets events relating to a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="Offset">Offset </param>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <returns>Token events.</returns>
		public Task<TokenEvent[]> GetNeuroFeatureEvents(string TokenId, int Offset, int MaxCount)
		{
			return this.NeuroFeaturesClient.GetEventsAsync(TokenId, Offset, MaxCount);
		}

		/// <summary>
		/// Adds a text note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="TextNote">Text Note</param>
		public Task AddNeuroFeatureTextNote(string TokenId, string TextNote)
		{
			return this.AddNeuroFeatureTextNote(TokenId, TextNote, false);
		}

		/// <summary>
		/// Adds a text note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="TextNote">Text Note</param>
		/// <param name="Personal">If the text note contains personal information. (default=false).
		///
		/// Note: Personal notes are deleted when ownership of token is transferred.</param>
		public Task AddNeuroFeatureTextNote(string TokenId, string TextNote, bool Personal)
		{
			this.lastTokenEvent = DateTime.Now;

			return this.NeuroFeaturesClient.AddTextNoteAsync(TokenId, TextNote, Personal);
		}

		/// <summary>
		/// Adds a xml note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="XmlNote">Xml Note</param>
		public Task AddNeuroFeatureXmlNote(string TokenId, string XmlNote)
		{
			return this.AddNeuroFeatureXmlNote(TokenId, XmlNote, false);
		}

		/// <summary>
		/// Adds a xml note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="XmlNote">Xml Note</param>
		/// <param name="Personal">If the xml note contains personal information. (default=false).
		///
		/// Note: Personal notes are deleted when ownership of token is transferred.</param>
		public Task AddNeuroFeatureXmlNote(string TokenId, string XmlNote, bool Personal)
		{
			this.lastTokenEvent = DateTime.Now;

			return this.NeuroFeaturesClient.AddXmlNoteAsync(TokenId, XmlNote, Personal);
		}

		/// <summary>
		/// Gets token creation attributes from the broker.
		/// </summary>
		/// <returns>Token creation attributes.</returns>
		public Task<CreationAttributesEventArgs> GetNeuroFeatureCreationAttributes()
		{
			return this.NeuroFeaturesClient.GetCreationAttributesAsync();
		}

		/// <summary>
		/// Generates a XAML report for a state diagram corresponding to the token.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public async Task<string> GenerateNeuroFeatureStateDiagramReport(string TokenId)
		{
			ReportEventArgs e = await this.NeuroFeaturesClient.GenerateStateDiagramAsync(TokenId, ReportFormat.XamarinXaml);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetStateDiagram"]);

			return e.ReportText;
		}

		/// <summary>
		/// Generates a XAML report for a timing diagram corresponding to the token.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public async Task<string> GenerateNeuroFeatureProfilingReport(string TokenId)
		{
			ReportEventArgs e = await this.NeuroFeaturesClient.GenerateProfilingReportAsync(TokenId, ReportFormat.XamarinXaml);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetProfiling"]);

			return e.ReportText;
		}

		/// <summary>
		/// Generates a XAML present report for a token.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public async Task<string> GenerateNeuroFeaturePresentReport(string TokenId)
		{
			ReportEventArgs e = await this.NeuroFeaturesClient.GeneratePresentReportAsync(TokenId, ReportFormat.XamarinXaml);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetPresent"]);

			return e.ReportText;
		}

		/// <summary>
		/// Generates a XAML history report for a token.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public async Task<string> GenerateNeuroFeatureHistoryReport(string TokenId)
		{
			ReportEventArgs e = await this.NeuroFeaturesClient.GenerateHistoryReportAsync(TokenId, ReportFormat.XamarinXaml);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetHistory"]);

			return e.ReportText;
		}

		/// <summary>
		/// Gets the current state of a Neuro-Feature token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Current state</returns>
		public Task<CurrentStateEventArgs> GetNeuroFeatureCurrentState(string TokenId)
		{
			return this.NeuroFeaturesClient.GetCurrentStateAsync(TokenId);
		}

		#endregion

	}
}
