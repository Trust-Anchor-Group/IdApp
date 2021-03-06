﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using EDaler;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class NeuronService : LoadableService, INeuronService
	{
		private readonly Assembly appAssembly;
		private readonly INetworkService networkService;
		private readonly ILogService logService;
		private readonly ITagProfile tagProfile;
		private readonly ISettingsService settingsService;
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
		private Timer reconnectTimer;
		private readonly NeuronContracts contracts;
		private readonly NeuronMultiUserChat muc;
		private readonly NeuronThingRegistry thingRegistry;
		private readonly NeuronProvisioningService provisioning;
		private readonly NeuronWallet wallet;
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

		public NeuronService(
			Assembly appAssembly,
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INetworkService networkService,
			ILogService logService,
			ISettingsService settingsService,
			Profiler startupProfiler)
		{
			this.appAssembly = appAssembly;
			this.networkService = networkService;
			this.logService = logService;
			this.tagProfile = tagProfile;
			this.settingsService = settingsService;
			this.contracts = new NeuronContracts(this.tagProfile, uiDispatcher, this, this.logService, this.settingsService);
			this.muc = new NeuronMultiUserChat(this);
			this.thingRegistry = new NeuronThingRegistry(this);
			this.provisioning = new NeuronProvisioningService(this);
			this.wallet = new NeuronWallet(this, this.logService);
			this.sniffer = new InMemorySniffer(250);
			this.startupProfiler = startupProfiler;
		}

		#region Create/Destroy

		private async Task CreateXmppClient(bool CanCreateKeys, ProfilerThread Thread)
		{
			if (isCreatingClient)
				return;

			this.xmppThread = this.startupProfiler?.CreateThread("XMPP", ProfilerThreadType.StateMachine);
			this.xmppThread?.Start();
			this.xmppThread?.Idle();

			try
			{
				isCreatingClient = true;

				if (!this.XmppParametersCurrent() || this.XmppStale())
				{
					if (!(this.xmppClient is null))
					{
						Thread?.NewState("Destroy");
						this.DestroyXmppClient();
					}

					this.domainName = this.tagProfile.Domain;
					this.accountName = this.tagProfile.Account;
					this.passwordHash = this.tagProfile.PasswordHash;
					this.passwordHashMethod = this.tagProfile.PasswordHashMethod;

					string HostName;
					int PortNumber;
					bool IsIpAddress;

					if (this.tagProfile.DefaultXmppConnectivity)
					{
						HostName = this.domainName;
						PortNumber = XmppCredentials.DefaultPort;
						IsIpAddress = false;
					}
					else
					{
						Thread?.NewState("DNS");
						(HostName, PortNumber, IsIpAddress) = await this.networkService.LookupXmppHostnameAndPort(domainName);

						if (HostName == domainName && PortNumber == XmppCredentials.DefaultPort)
							this.tagProfile.SetDomain(domainName, true);
					}

					this.xmppLastStateChange = DateTime.Now;
					this.xmppConnected = false;

					Thread?.NewState("Client");
					this.xmppClient = new XmppClient(HostName, PortNumber, accountName, passwordHash, passwordHashMethod, Constants.LanguageCodes.Default, appAssembly, this.sniffer)
					{
						TrustServer = !IsIpAddress,
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

					Thread?.NewState("Sink");
					this.xmppEventSink = new XmppEventSink("XMPP Event Sink", this.xmppClient, this.tagProfile.LogJid, false);

					// Add extensions before connecting

					if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
					{
						Thread?.NewState("Legal");
						this.contractsClient = new ContractsClient(this.xmppClient, this.tagProfile.LegalJid);

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

					if (!string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid) && this.tagProfile.HttpFileUploadMaxSize.HasValue)
					{
						Thread?.NewState("Upload");
						this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize);
					}

					if (!string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
					{
						Thread?.NewState("MUC");
						this.mucClient = new MultiUserChatClient(this.xmppClient, this.tagProfile.MucJid);
					}

					if (!string.IsNullOrWhiteSpace(this.tagProfile.RegistryJid))
					{
						Thread?.NewState("Reg");
						this.thingRegistryClient = new ThingRegistryClient(this.xmppClient, this.tagProfile.RegistryJid);
					}

					if (!string.IsNullOrWhiteSpace(this.tagProfile.ProvisioningJid))
					{
						Thread?.NewState("Prov");
						this.provisioningClient = new ProvisioningClient(this.xmppClient, this.tagProfile.ProvisioningJid);
					}

					if (!string.IsNullOrWhiteSpace(this.tagProfile.EDalerJid))
					{
						Thread?.NewState("eDaler");
						this.eDalerClient = new EDalerClient(this.xmppClient, this.Contracts.ContractsClient, this.tagProfile.EDalerJid);
					}

					Thread?.NewState("Sensor");
					this.sensorClient = new SensorClient(this.xmppClient);

					Thread?.NewState("Control");
					this.controlClient = new ControlClient(this.xmppClient);

					Thread?.NewState("Concentrator");
					this.concentratorClient = new ConcentratorClient(this.xmppClient);

					Thread?.NewState("Connect");
					this.IsLoggedOut = false;
					this.xmppClient.Connect(IsIpAddress ? string.Empty : domainName);
					this.RecreateReconnectTimer();

					// Await connected state during registration or user initiated log in, but not otherwise.
					if (!this.tagProfile.IsCompleteOrWaitingForValidation())
					{
						Thread?.NewState("Wait");
						if (!await this.WaitForConnectedState(Constants.Timeouts.XmppConnect))
						{
							this.logService.LogWarning("Connect to XMPP server '{0}' failed for account '{1}' with the specified timeout of {2} ms",
								this.domainName,
								this.accountName,
								(int)Constants.Timeouts.XmppConnect.TotalMilliseconds);
						}
					}
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
			this.reconnectTimer = null;

			this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(XmppState.Offline));

			if (!(this.xmppEventSink is null))
			{
				this.logService.RemoveListener(this.xmppEventSink);
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

			this.sensorClient?.Dispose();
			this.sensorClient = null;

			this.controlClient?.Dispose();
			this.controlClient = null;

			this.concentratorClient?.Dispose();
			this.concentratorClient = null;

			this.xmppClient?.Dispose();
			this.xmppClient = null;
		}

		private bool XmppStale()
		{
			return this.xmppClient is null ||
				this.xmppClient.State == XmppState.Offline ||
				this.xmppClient.State == XmppState.Error ||
				(this.xmppClient.State != XmppState.Connected && (DateTime.Now - this.xmppLastStateChange).TotalSeconds > 10);
		}

		private bool XmppParametersCurrent()
		{
			if (this.xmppClient is null)
				return false;

			if (this.domainName != this.tagProfile.Domain) 
				return false;

			if (this.accountName != this.tagProfile.Account) 
				return false;

			if (this.passwordHash != this.tagProfile.PasswordHash) 
				return false;

			if (this.passwordHashMethod != this.tagProfile.PasswordHashMethod) 
				return false;

			if (this.contractsClient?.ComponentAddress != this.tagProfile.LegalJid) 
				return false;

			if (this.fileUploadClient?.FileUploadJid != this.tagProfile.HttpFileUploadJid) 
				return false;

			if (this.mucClient?.ComponentAddress != this.tagProfile.MucJid) 
				return false;

			if (this.thingRegistryClient?.ThingRegistryAddress != this.tagProfile.RegistryJid)
				return false;

			if (this.provisioningClient?.ProvisioningServerAddress != this.tagProfile.ProvisioningJid)
				return false;

			if (this.eDalerClient?.ComponentAddress != this.tagProfile.EDalerJid)
				return false;

			return true;
		}

		private bool ShouldCreateClient()
		{
			return this.tagProfile.Step > RegistrationStep.Account && !this.XmppParametersCurrent();
		}

		private void RecreateReconnectTimer()
		{
			this.reconnectTimer?.Dispose();
			this.reconnectTimer = new Timer(ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);
		}

		#endregion

		private async void TagProfile_StepChanged(object sender, EventArgs e)
		{
			if (!this.IsLoaded)
				return;

			if (ShouldCreateClient())
				await this.CreateXmppClient(this.tagProfile.Step <= RegistrationStep.RegisterIdentity, null);
			else if (this.tagProfile.Step <= RegistrationStep.Account)
				this.DestroyXmppClient();
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

					if (this.tagProfile.NeedsUpdating() && await this.DiscoverServices())
					{
						if (this.contractsClient is null && !string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
						{
							this.contractsClient = new ContractsClient(this.xmppClient, this.tagProfile.LegalJid);

							if (!await this.contractsClient.LoadKeys(false))
							{
								this.contractsClient.Dispose();
								this.contractsClient = null;
							}
						}

						if (this.fileUploadClient is null && !string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid) && this.tagProfile.HttpFileUploadMaxSize.HasValue)
							this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize);

						if (this.mucClient is null && !string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
							this.mucClient = new MultiUserChatClient(this.xmppClient, this.tagProfile.MucJid);

						if (this.thingRegistryClient is null && !string.IsNullOrWhiteSpace(this.tagProfile.RegistryJid))
							this.thingRegistryClient = new ThingRegistryClient(this.xmppClient, this.tagProfile.RegistryJid);

						if (this.provisioningClient is null && !string.IsNullOrWhiteSpace(this.tagProfile.RegistryJid))
							this.provisioningClient = new ProvisioningClient(this.xmppClient, this.tagProfile.ProvisioningJid);

						if (this.eDalerClient is null && !string.IsNullOrWhiteSpace(this.tagProfile.EDalerJid))
							this.eDalerClient = new EDalerClient(this.xmppClient, this.Contracts.ContractsClient, this.tagProfile.EDalerJid);
					}

					this.logService.AddListener(this.xmppEventSink);

					this.xmppThread?.Stop();
					this.xmppThread = null;
					this.startupProfiler = null;
					break;

				case XmppState.Offline:
				case XmppState.Error:
					if (this.xmppConnected)
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

		public async Task<bool> WaitForConnectedState(TimeSpan timeout)
		{
			if (this.xmppClient is null)
				return false;

			if (this.xmppClient.State == XmppState.Connected)
				return true;

			int i = await this.xmppClient.WaitStateAsync((int)timeout.TotalMilliseconds, XmppState.Connected);
			return i >= 0;
		}

		public override async Task Load(bool isResuming)
		{
			ProfilerThread Thread = this.startupProfiler?.CreateThread("Neuron", ProfilerThreadType.Sequential);
			Thread?.Start();
			try
			{
				if (this.BeginLoad())
				{
					Thread?.NewState("Load");
					try
					{
						this.tagProfile.StepChanged += TagProfile_StepChanged;

						if (ShouldCreateClient())
						{
							Thread?.NewState("XMPP");

							ProfilerThread ClientsThread = Thread?.CreateSubThread("Clients", ProfilerThreadType.Sequential);
							ClientsThread?.Start();
							try
							{
								await this.CreateXmppClient(this.tagProfile.Step <= RegistrationStep.RegisterIdentity, ClientsThread);
							}
							finally
							{
								ClientsThread?.Stop();
							}
						}

						if (!(this.xmppClient is null) &&
							this.xmppClient.State == XmppState.Connected &&
							this.tagProfile.IsCompleteOrWaitingForValidation())
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
						this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
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
					this.tagProfile.StepChanged -= TagProfile_StepChanged;

					if (!(this.xmppClient is null) && !fast)
					{
						try
						{
							await this.xmppClient.SetPresenceAsync(Availability.Offline);
						}
						catch (Exception)
						{
							// Ignore
						}
					}

					this.DestroyXmppClient();
				}
				catch (Exception ex)
				{
					this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				this.EndUnload();
			}
		}

		public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

		private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
		{
			this.ConnectionStateChanged?.Invoke(this, e);
		}

		#endregion

		#region State

		public bool IsLoggedOut { get; private set; }
		public bool IsOnline => !(this.xmppClient is null) && this.xmppClient.State == XmppState.Connected;
		public XmppState State => this.xmppClient?.State ?? XmppState.Offline;
		public string BareJid => xmppClient?.BareJID ?? string.Empty;

		public string LatestError { get; private set; }
		public string LatestConnectionError { get; private set; }

		public XmppClient Xmpp => this.xmppClient;
		public ContractsClient ContractsClient => this.contractsClient;
		public HttpFileUploadClient FileUploadClient => this.fileUploadClient;
		public MultiUserChatClient MucClient => this.mucClient;
		public ThingRegistryClient ThingRegistryClient => this.thingRegistryClient;
		public ProvisioningClient ProvisioningClient => this.provisioningClient;
		public ControlClient ControlClient => this.controlClient;
		public SensorClient SensorClient => this.sensorClient;
		public ConcentratorClient ConcentratorClient => this.concentratorClient;
		public EDalerClient EDalerClient => this.eDalerClient;

		#endregion

		public INeuronContracts Contracts => this.contracts;
		public INeuronMultiUserChat MultiUserChat => this.muc;
		public INeuronThingRegistry ThingRegistry => this.thingRegistry;
		public INeuronProvisioningService Provisioning => this.provisioning;
		public INeuronWallet Wallet => this.wallet;

		private enum ConnectOperation
		{
			Connect,
			ConnectAndCreateAccount,
			ConnectAndConnectToAccount
		}

		public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, bool isIpAddress, string hostName, int portNumber,
			string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
		{
			return TryConnectInner(domain, isIpAddress, hostName, portNumber, string.Empty, string.Empty, languageCode,
				applicationAssembly, connectedFunc, ConnectOperation.Connect);
		}

		public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, bool isIpAddress, string hostName,
			int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly,
			Func<XmppClient, Task> connectedFunc)
		{
			return TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, languageCode, applicationAssembly,
				connectedFunc, ConnectOperation.ConnectAndCreateAccount);
		}

		public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, bool isIpAddress, string hostName,
			int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly,
			Func<XmppClient, Task> connectedFunc)
		{
			return TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, languageCode, applicationAssembly,
				connectedFunc, ConnectOperation.ConnectAndConnectToAccount);
		}

		private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, bool isIpAddress, string hostName,
			int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly,
			Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
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

					client.TrustServer = !isIpAddress;
					client.AllowCramMD5 = false;
					client.AllowDigestMD5 = false;
					client.AllowPlain = false;
					client.AllowEncryption = true;
					client.AllowScramSHA1 = true;

					client.OnConnectionError += OnConnectionError;
					client.OnStateChanged += OnStateChanged;

					client.Connect(isIpAddress ? string.Empty : domain);

					void TimerCallback(object _)
					{
						timeout = true;
						connected.TrySetResult(false);
					}

					using (Timer _ = new Timer(TimerCallback, null, (int)Constants.Timeouts.XmppConnect.TotalMilliseconds, Timeout.Infinite))
					{
						succeeded = await connected.Task;
					}

					if (succeeded && !(connectedFunc is null))
						await connectedFunc(client);

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
						errorMessage = connectionError;
					else
						errorMessage = string.Format(AppResources.OperatorDoesNotSupportRegisteringNewAccounts, domain);
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

		public async Task<bool> DiscoverServices(XmppClient client = null)
		{
			client = client ?? xmppClient;
			if (client is null)
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
					this.tagProfile.SetLegalJid(item.JID);

				if (itemResponse.HasFeature(ThingRegistryClient.NamespaceDiscovery))
					this.tagProfile.SetRegistryJid(item.JID);

				if (itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningDevice) &&
					itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningOwner) &&
					itemResponse.HasFeature(ProvisioningClient.NamespaceProvisioningToken))
				{
					this.tagProfile.SetProvisioningJid(item.JID);
				}

				if (itemResponse.HasFeature(HttpFileUploadClient.Namespace))
				{
					long? maxSize = HttpFileUploadClient.FindMaxFileSize(client, itemResponse);
					this.tagProfile.SetFileUploadParameters(item.JID, maxSize);
				}

				if (itemResponse.HasFeature(XmppEventSink.NamespaceEventLogging))
					this.tagProfile.SetLogJid(item.JID);

				if (itemResponse.HasFeature(MultiUserChatClient.NamespaceMuc))
					this.tagProfile.SetMucJid(item.JID);

				if (itemResponse.HasFeature(EDalerClient.NamespaceEDaler))
					this.tagProfile.SetEDalerJid(item.JID);
			}

			if (string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid) || !this.tagProfile.HttpFileUploadMaxSize.HasValue)
				return false;

			if (string.IsNullOrWhiteSpace(this.tagProfile.LogJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
				return false;

			if (string.IsNullOrWhiteSpace(this.tagProfile.EDalerJid))
				return false;

			return true;
		}

		private void ReconnectTimer_Tick(object _)
		{
			if (this.xmppClient is null)
				return;

			if (!this.networkService.IsOnline)
				return;

			if (this.XmppStale())
			{
				this.xmppLastStateChange = DateTime.Now;
				this.xmppClient.Reconnect();
			}
		}

		public void ClearHtmlContent()
		{
			historyHtml = sentHtml;
			historyTextData = sentTextData;
		}

		public string CommsDumpAsText(string state)
		{
			string response;

			if (historyTextData is null || state != "History")
				response = sniffer.SnifferToText();
			else
				response = sniffer.SnifferToText().Replace(historyTextData, "");

			return response;
		}


		public string CommsDumpAsHtml(bool history = false)
		{
			string html = string.Empty;

			try
			{
				string xml = sniffer.SnifferToXml();

				sentHtml = xml;
				sentTextData = sniffer.SnifferToText();

				if (!(historyHtml is null) && !history)
				{
					xml = xml.Replace(historyHtml, "");
				}

				xml = $"<SnifferOutput>{xml}</SnifferOutput>";

				//adding coloring to debug lines
				html = cssColoring + FixTags(xml);
			}
			catch (Exception e)
			{
				this.logService.LogException(e);
			}

			return html;
		}

		//fixing broken tags
		private static string FixTags(string xml)
		{
			return xml.Replace("&lt;", "<").Replace("&gt;", ">");
		}
	}
}