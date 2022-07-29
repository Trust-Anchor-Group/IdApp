using EDaler;
using IdApp.Extensions;
using IdApp.Pages.Contacts.Chat;
using IdApp.Popups.Xmpp.ReportOrBlock;
using IdApp.Popups.Xmpp.ReportType;
using IdApp.Popups.Xmpp.SubscribeTo;
using IdApp.Popups.Xmpp.SubscriptionRequest;
using IdApp.Resx;
using IdApp.Services.Contracts;
using IdApp.Services.Messages;
using IdApp.Services.Navigation;
using IdApp.Services.Push;
using IdApp.Services.Provisioning;
using IdApp.Services.Tag;
using IdApp.Services.ThingRegistries;
using IdApp.Services.UI.Photos;
using IdApp.Services.Wallet;
using NeuroFeatures;
using System;
using System.Collections.Generic;
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
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.Push;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Waher.Runtime.Settings;
using Xamarin.Forms;
using IdApp.Pages.Registration.Registration;

namespace IdApp.Services.Xmpp
{
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
		private Timer reconnectTimer;
		private readonly SmartContracts contracts;
		private readonly XmppMultiUserChat muc;
		private readonly XmppThingRegistry thingRegistry;
		private readonly XmppProvisioningService provisioning;
		private readonly NeuroWallet wallet;
		private string domainName;
		private string accountName;
		private string passwordHash;
		private string passwordHashMethod;
		private bool xmppConnected = false;
		private DateTime xmppLastStateChange = DateTime.MinValue;
		private readonly InMemorySniffer sniffer;
		private bool isCreatingClient;
		private XmppEventSink xmppEventSink;
		private readonly string cssColoring = "<style type='text/css'>* {word-wrap: break-word } info { color: #ffffff; background-color: #000080; display: block;} warning { background-color: #F8DE7E; display: block;} error {background-color: #FF0000;display: block; } Rx {color: #ffffff; background-color: #008000;display: block;} ping:empty:before { content: 'ping ';} iq:empty:before { content: attr(type) ' ' attr(to);} c:empty:before { content: attr(node);} session:empty:before, stream:empty:before, starttls:empty:before, proceed:empty:before { content: attr(xmlns);}</style>";
		private string sentHtml;
		private string sentTextData;
		private string historyTextData;
		private string historyHtml;

		public XmppService(Assembly AppAssembly, Profiler StartupProfiler)
		{
			this.appAssembly = AppAssembly;
			this.contracts = new SmartContracts();
			this.muc = new XmppMultiUserChat();
			this.thingRegistry = new XmppThingRegistry();
			this.provisioning = new XmppProvisioningService();
			this.wallet = new NeuroWallet();
			this.sniffer = new InMemorySniffer(250);
			this.startupProfiler = StartupProfiler;
		}

		#region Create/Destroy

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
						this.DestroyXmppClient();
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
							this.TagProfile.SetDomain(this.domainName, true, this.TagProfile.ApiKey, this.TagProfile.ApiSecret);
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

					this.xmppClient.RequestRosterOnStartup = true;
					this.xmppClient.OnStateChanged += this.XmppClient_StateChanged;
					this.xmppClient.OnConnectionError += this.XmppClient_ConnectionError;
					this.xmppClient.OnError += this.XmppClient_Error;
					this.xmppClient.OnChatMessage += this.XmppClient_OnChatMessage;
					this.xmppClient.OnNormalMessage += this.XmppClient_OnNormalMessage;
					this.xmppClient.OnPresenceSubscribe += this.XmppClient_OnPresenceSubscribe;
					this.xmppClient.OnPresenceUnsubscribed += this.XmppClient_OnPresenceUnsubscribed;

					this.xmppClient.RegisterMessageHandler("Delivered", ContractsClient.NamespaceOnboarding, this.TransferIdDelivered, true);

					Thread?.NewState("Sink");
					this.xmppEventSink = new XmppEventSink("XMPP Event Sink", this.xmppClient, this.TagProfile.LogJid, false);

					// Add extensions before connecting

					this.abuseClient = new AbuseClient(this.xmppClient);

					if (!string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
					{
						Thread?.NewState("Legal");
						this.contractsClient = new ContractsClient(this.xmppClient, this.TagProfile.LegalJid);

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
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.EDalerJid))
					{
						Thread?.NewState("eDaler");
						this.eDalerClient = new EDalerClient(this.xmppClient, this.Contracts.ContractsClient, this.TagProfile.EDalerJid);
					}

					if (!string.IsNullOrWhiteSpace(this.TagProfile.NeuroFeaturesJid))
					{
						Thread?.NewState("Neuro-Features");
						this.neuroFeaturesClient = new NeuroFeaturesClient(this.xmppClient, this.Contracts.ContractsClient, this.TagProfile.NeuroFeaturesJid);
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
							this.LogService.LogWarning("Connect to XMPP server '{0}' failed for account '{1}' with the specified timeout of {2} ms",
								this.domainName,
								this.accountName,
								(int)Constants.Timeouts.XmppConnect.TotalMilliseconds);
						}
					}
				}
			}
			finally
			{
				this.isCreatingClient = false;
			}
		}

		private Task XmppClient_OnNormalMessage(object Sender, MessageEventArgs e)
		{
			this.LogService.LogWarning("Unhandled message received.", e.To, e.From,
				new KeyValuePair<string, object>("Stanza", e.Message.OuterXml));

			return Task.CompletedTask;
		}

		private void DestroyXmppClient()
		{
			this.reconnectTimer?.Dispose();
			this.reconnectTimer = null;

			this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(XmppState.Offline));

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

		private async void TagProfile_StepChanged(object sender, EventArgs e)
		{
			if (!this.IsLoaded)
				return;

			if (this.ShouldCreateClient())
				await this.CreateXmppClient(this.TagProfile.Step <= RegistrationStep.RegisterIdentity, null);
			else if (this.TagProfile.Step <= RegistrationStep.Account)
				this.DestroyXmppClient();
		}

		private Task XmppClient_Error(object sender, Exception e)
		{
			this.LatestError = e.Message;
			return Task.CompletedTask;
		}

		private Task XmppClient_ConnectionError(object sender, Exception e)
		{
			if (e is ObjectDisposedException)
				this.LatestConnectionError = AppResources.UnableToConnect;
			else
				this.LatestConnectionError = e.Message;

			return Task.CompletedTask;
		}

		private async Task XmppClient_StateChanged(object sender, XmppState newState)
		{
			this.xmppLastStateChange = DateTime.Now;

			this.xmppThread?.NewState(newState.ToString());

			switch (newState)
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
						this.TagProfile.SetAccount(this.TagProfile.Account, this.xmppClient.PasswordHash, this.xmppClient.PasswordHashMethod);

					if (this.TagProfile.NeedsUpdating() && await this.DiscoverServices())
					{
						if (this.contractsClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
						{
							this.contractsClient = new ContractsClient(this.xmppClient, this.TagProfile.LegalJid);

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
						}

						if (this.eDalerClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.EDalerJid))
							this.eDalerClient = new EDalerClient(this.xmppClient, this.Contracts.ContractsClient, this.TagProfile.EDalerJid);

						if (this.neuroFeaturesClient is null && !string.IsNullOrWhiteSpace(this.TagProfile.NeuroFeaturesJid))
							this.neuroFeaturesClient = new NeuroFeaturesClient(this.xmppClient, this.Contracts.ContractsClient, this.TagProfile.NeuroFeaturesJid);

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
						this.xmppClient?.Reconnect();
					}

					this.xmppThread?.Stop();
					this.xmppThread = null;
					this.startupProfiler = null;
					break;
			}

			this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(newState));
		}

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

					this.DestroyXmppClient();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				this.EndUnload();
			}
		}

		public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

		private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
		{
			try
			{
				this.ConnectionStateChanged?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
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

		public XmppClient Xmpp => this.xmppClient;
		public HttpFileUploadClient FileUploadClient => this.fileUploadClient;
		public MultiUserChatClient MucClient => this.mucClient;
		public ThingRegistryClient ThingRegistryClient => this.thingRegistryClient;
		public ProvisioningClient ProvisioningClient => this.provisioningClient;
		public ControlClient ControlClient => this.controlClient;
		public SensorClient SensorClient => this.sensorClient;
		public ConcentratorClient ConcentratorClient => this.concentratorClient;
		public EDalerClient EDalerClient => this.eDalerClient;
		public NeuroFeaturesClient NeuroFeaturesClient => this.neuroFeaturesClient;
		public PushNotificationClient PushNotificationClient => this.pushNotificationClient;
		public ContractsClient ContractsClient => this.contractsClient;

		#endregion

		public ISmartContracts Contracts => this.contracts;
		public IXmppMultiUserChat MultiUserChat => this.muc;
		public IXmppThingRegistry ThingRegistry => this.thingRegistry;
		public IXmppProvisioningService Provisioning => this.provisioning;
		public INeuroWallet Wallet => this.wallet;

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
					connectionError = AppResources.UnableToConnect;
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
				this.LogService.LogException(ex, new KeyValuePair<string, string>(nameof(ConnectOperation), operation.ToString()));
				succeeded = false;
				errorMessage = string.Format(AppResources.UnableToConnectTo, domain);
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
						errorMessage = connectionError;
					else
						errorMessage = string.Format(AppResources.OperatorDoesNotSupportRegisteringNewAccounts, domain);
				}
				else if (operation == ConnectOperation.ConnectAndCreateAccount)
					errorMessage = string.Format(AppResources.AccountNameAlreadyTaken, this.accountName);
				else if (operation == ConnectOperation.ConnectToAccount)
					errorMessage = string.Format(AppResources.InvalidUsernameOrPassword, this.accountName);
				else
					errorMessage = string.Format(AppResources.UnableToConnectTo, domain);
			}

			return (succeeded, errorMessage);
		}

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
				this.LogService.LogException(ex, new KeyValuePair<string, string>("Sniffer", commsDump));
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

		private void ReconnectTimer_Tick(object _)
		{
			if (this.xmppClient is null)
				return;

			if (!this.NetworkService.IsOnline)
				return;

			if (this.XmppStale())
			{
				this.xmppLastStateChange = DateTime.Now;
				this.xmppClient.Reconnect();
			}
		}

		public void ClearHtmlContent()
		{
			this.historyHtml = this.sentHtml;
			this.historyTextData = this.sentTextData;
		}

		public async Task<string> CommsDumpAsText(string state)
		{
			string response;

			if (this.historyTextData is null || state != "History")
				response = await this.sniffer.SnifferToText();
			else
				response = (await this.sniffer.SnifferToText()).Replace(this.historyTextData, "");

			return response;
		}


		public async Task<string> CommsDumpAsHtml(bool history = false)
		{
			string html = string.Empty;

			try
			{
				string xml = await this.sniffer.SnifferToXml();

				this.sentHtml = xml;
				this.sentTextData = await this.sniffer.SnifferToText();

				if ((this.historyHtml is not null) && !history)
				{
					xml = xml.Replace(this.historyHtml, "");
				}

				xml = "<SnifferOutput>" + xml + "</SnifferOutput>";

				//adding coloring to debug lines
				html = this.cssColoring + FixTags(xml);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}

			return html;
		}

		//fixing broken tags
		private static string FixTags(string xml)
		{
			return xml.Replace("&lt;", "<").Replace("&gt;", ">");
		}

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

			this.DestroyXmppClient();

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

		private async Task XmppClient_OnChatMessage(object Sender, MessageEventArgs e)
		{
			ContactInfo ContactInfo = await ContactInfo.FindByBareJid(e.FromBareJID);
			string FriendlyName = ContactInfo?.FriendlyName ?? e.FromBareJID;
			string ReplaceObjectId = null;

			ChatMessage Message = new()
			{
				Created = DateTime.UtcNow,
				RemoteBareJid = e.FromBareJID,
				RemoteObjectId = e.Id,
				MessageType = Messages.MessageType.Received,
				Html = "",
				PlainText = e.Body,
				Markdown = ""
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
					Log.Critical(ex);
					Message.Markdown = string.Empty;
				}
			}

			if (string.IsNullOrEmpty(ReplaceObjectId))
				await Database.Insert(Message);
			else
			{
				ChatMessage Old = await Database.FindFirstIgnoreRest<ChatMessage>(new FilterAnd(
					new FilterFieldEqualTo("RemoteBareJid", e.FromBareJID),
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

			INavigationService NavigationService = App.Instantiate<INavigationService>();

			if (NavigationService.TryPopArgs(out ChatNavigationArgs args, e.FromBareJID))
			{
				if ((NavigationService.CurrentPage is ChatPage || NavigationService.CurrentPage is ChatPageIos) &&
					NavigationService.CurrentPage.BindingContext is ChatViewModel ChatViewModel &&
					ChatViewModel.BareJid == e.FromBareJID)
				{
					if (string.IsNullOrEmpty(ReplaceObjectId))
						await ChatViewModel.MessageAddedAsync(Message);
					else
						await ChatViewModel.MessageUpdatedAsync(Message);
				}
			}
			else
			{
				string LegalId = ContactInfo?.LegalId;
				string BareJid = ContactInfo?.BareJid ?? e.FromBareJID;

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
					await NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(LegalId, BareJid, FriendlyName) { UniqueId = BareJid }));

				Thread.Sleep(100);
			}
		}

		#region Push Notification

		/// <summary>
		/// Registers a new token with the back-end broker.
		/// </summary>
		/// <param name="TokenInformation">Token information.</param>
		/// <returns>If token could be registered.</returns>
		public async Task<bool> NewPushNotificationToken(TokenInformation TokenInformation)
		{
			// TODO: Check if started

			DateTime TP = DateTime.UtcNow;

			await RuntimeSettings.SetAsync("PUSH.TOKEN", TokenInformation.Token);
			await RuntimeSettings.SetAsync("PUSH.SERVICE", TokenInformation.Service);
			await RuntimeSettings.SetAsync("PUSH.CLIENT", TokenInformation.ClientType);
			await RuntimeSettings.SetAsync("PUSH.TP", TP);

			if (this.pushNotificationClient is null || !this.IsOnline)
				return false;
			else
			{
				await this.pushNotificationClient.NewTokenAsync(TokenInformation.Token, TokenInformation.Service, TokenInformation.ClientType);
				await RuntimeSettings.SetAsync("PUSH.LAST_TP", TP);

				return true;
			}
		}

		#endregion
	}
}
