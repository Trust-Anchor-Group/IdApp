using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Tag.Neuron.Xamarin.Extensions;
using EDaler;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
	internal sealed class NeuronService : LoadableService, IInternalNeuronService
	{
		private readonly Assembly appAssembly;
		private readonly INetworkService networkService;
		private readonly ILogService logService;
		private readonly ITagProfile tagProfile;
		private readonly ISettingsService settingsService;
		private Timer reconnectTimer;
		private XmppClient xmppClient;
		private Profiler startupProfiler;
		private ProfilerThread xmppThread;
		private readonly NeuronContracts contracts;
		private readonly NeuronMultiUserChat muc;
		private readonly NeuronThingRegistry thingRegistry;
		private readonly NeuronWallet wallet;
		private string domainName;
		private string accountName;
		private string passwordHash;
		private string passwordHashMethod;
		private bool xmppSettingsOk;
		private readonly InMemorySniffer sniffer;
		private bool isCreatingClient;
		private XmppEventSink xmppEventSink;
		private bool userInitiatedLogInOrOut;
		private readonly string cssColoring = "<style type='text/css'>* {word-wrap: break-word } info { color: #ffffff; background-color: #000080; display: block;} warning { background-color: #F8DE7E; display: block;} error {background-color: #FF0000;display: block; } Tx {color: #ffffff; background-color: #008000;display: block;} </style>";
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
			this.muc = new NeuronMultiUserChat(this.tagProfile, this);
			this.thingRegistry = new NeuronThingRegistry(this);
			this.wallet = new NeuronWallet(this, this.logService);
			this.sniffer = new InMemorySniffer(250);
			this.startupProfiler = startupProfiler;
		}

		#region Create/Destroy

		private async Task CreateXmppClient(bool CanCreateKeys)
		{
			this.xmppThread = this.startupProfiler?.CreateThread("XMPP", ProfilerThreadType.StateMachine);
			this.xmppThread?.Start();
			this.xmppThread?.Idle();

			if (isCreatingClient)
				return;

			try
			{
				isCreatingClient = true;

				if (!(this.xmppClient is null))
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

					(string hostName, int portNumber, bool isIpAddress) = await this.networkService.LookupXmppHostnameAndPort(domainName);

					this.xmppClient = new XmppClient(hostName, portNumber, accountName, passwordHash, passwordHashMethod, Constants.LanguageCodes.Default, appAssembly, this.sniffer)
					{
						TrustServer = !isIpAddress,
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
                    this.xmppEventSink = new XmppEventSink("XMPP Event Sink", this.xmppClient, this.tagProfile.LogJid, false);

                    if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
                    {
                        await this.contracts.CreateClients(CanCreateKeys);
                    }

					this.IsLoggedOut = false;
					this.xmppClient.Connect(isIpAddress ? string.Empty : domainName);

                    bool connectSucceeded = false;
					// Await connected state during registration or user initiated log in, but not otherwise.
					if (!this.tagProfile.IsCompleteOrWaitingForValidation() || this.userInitiatedLogInOrOut)
					{
						connectSucceeded = await this.WaitForConnectedState(Constants.Timeouts.XmppConnect);
					}
					// This saves startup time for registered users with a complete profile
					if (this.tagProfile.IsComplete())
					{
						connectSucceeded = true;
					}

					if (!connectSucceeded)
                    {
						this.logService.LogWarning("Connect to XMPP server '{0}' failed for account '{1}' with the specified timeout of {2} ms",
							this.domainName,
							this.accountName,
							(int)Constants.Timeouts.XmppConnect.TotalMilliseconds);
					}

					this.RecreateReconnectTimer();
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
			this.contracts.DestroyClients();
			if (!(this.xmppClient is null))
			{
				this.xmppClient.OnError -= XmppClient_Error;
				this.xmppClient.OnConnectionError -= XmppClient_ConnectionError;
				this.xmppClient.OnStateChanged -= XmppClient_StateChanged;
				this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(XmppState.Offline, this.userInitiatedLogInOrOut));
				if (!(this.xmppEventSink is null))
				{
					this.logService.RemoveListener(this.xmppEventSink);
					this.xmppEventSink.Dispose();
					this.xmppEventSink = null;
				}

				this.xmppClient.Dispose();
			}
			this.xmppClient = null;
		}

		private bool ShouldCreateClient()
		{
			return this.tagProfile.Step > RegistrationStep.Account &&
				   (this.xmppClient is null ||
					this.domainName != this.tagProfile.Domain ||
					this.accountName != this.tagProfile.Account ||
					this.passwordHash != this.tagProfile.PasswordHash ||
					this.passwordHashMethod != this.tagProfile.PasswordHashMethod);
		}

		private bool ShouldDestroyClient()
		{
			return this.tagProfile.Step <= RegistrationStep.Account && !(this.xmppClient is null);
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
			{
				await this.CreateXmppClient(this.tagProfile.Step <= RegistrationStep.RegisterIdentity);
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
			this.xmppThread?.NewState(newState.ToString());

			switch (newState)
			{
				case XmppState.Connected:
					this.LatestError = string.Empty;
					this.LatestConnectionError = string.Empty;

					this.xmppSettingsOk = true;

					this.RecreateReconnectTimer();

                    string legalJidBefore = this.tagProfile.LegalJid;
					if (this.tagProfile.NeedsUpdating())
					{
						await this.DiscoverServices();
					}
                    string legalJidAfter = this.tagProfile.LegalJid;

                    bool legalJidWasCleared = !string.IsNullOrWhiteSpace(legalJidBefore) && string.IsNullOrWhiteSpace(legalJidAfter);
                    bool legalJidIsValid = !string.IsNullOrWhiteSpace(legalJidAfter);
					bool legalJidHasChangedAndIsValid = legalJidIsValid && !string.Equals(legalJidBefore, legalJidAfter);

					// If LegalJid was cleared, or is different
					if (legalJidWasCleared || legalJidHasChangedAndIsValid)
                    {
                        this.contracts.DestroyClients();
                    }

					// If we have a valid Jid, and contracts isn't created yet.
					if (legalJidHasChangedAndIsValid || (legalJidIsValid && !this.contracts.IsOnline))
                    {
                        try
                        {
                            await this.contracts.CreateClients(false);
                        }
                        catch (Exception e)
                        {
                            this.logService.LogException(e);
                        }
                    }

					this.logService.AddListener(this.xmppEventSink);

					this.xmppThread?.Stop();
					this.xmppThread = null;
					this.startupProfiler = null;
					break;

				case XmppState.Error:
					if (this.xmppSettingsOk)
					{
						this.xmppSettingsOk = false;
						this.xmppClient?.Reconnect();
					}

					this.xmppThread?.Stop();
					this.xmppThread = null;
					this.startupProfiler = null;
					break;
			}

			this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(newState, this.userInitiatedLogInOrOut));
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
			if (this.BeginLoad())
			{
				try
				{
                    this.tagProfile.StepChanged += TagProfile_StepChanged;

					if (ShouldCreateClient())
					{
						await this.CreateXmppClient(false);
					}
					if (!(this.xmppClient is null) &&
						this.xmppClient.State == XmppState.Connected &&
						this.tagProfile.IsCompleteOrWaitingForValidation())
					{
						// Don't await this one, just fire and forget, to improve startup time.
						_ = this.xmppClient.SetPresenceAsync(Availability.Online);
					}
					this.EndLoad(true);
				}
				catch (Exception ex)
				{
					this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
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
                    this.tagProfile.StepChanged -= TagProfile_StepChanged;

					if (!(this.xmppClient is null) && !fast)
					{
						await this.xmppClient.SetPresenceAsync(Availability.Offline);
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

		public XmppClient Xmpp => this.xmppClient;

		public string LatestError { get; private set; }

		public string LatestConnectionError { get; private set; }

		public string BareJid => xmppClient?.BareJID ?? string.Empty;

		#endregion

		public INeuronContracts Contracts => this.contracts;
		public INeuronMultiUserChat MultiUserChat => this.muc;
		public INeuronThingRegistry ThingRegistry => this.thingRegistry;
		public INeuronWallet Wallet => this.wallet;

		private enum ConnectOperation
		{
			Connect,
			ConnectAndCreateAccount,
			ConnectAndConnectToAccount
		}

		public Task LogOut()
		{
			if (!this.IsLoggedOut)
			{
				this.userInitiatedLogInOrOut = true;
				try
				{
					this.IsLoggedOut = true;
					this.DestroyXmppClient();
				}
				catch (Exception e)
				{
					this.IsLoggedOut = false;
					this.logService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
				finally
				{
					this.userInitiatedLogInOrOut = false;
				}
			}
			return Task.CompletedTask;
		}

		public async Task LogIn()
		{
			if (this.IsLoggedOut && this.ShouldCreateClient())
			{
				this.userInitiatedLogInOrOut = true;
				try
				{
					this.IsLoggedOut = false;
					await this.CreateXmppClient(false);
				}
				catch (Exception e)
				{
					this.IsLoggedOut = true;
					this.logService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
				finally
				{
					this.userInitiatedLogInOrOut = false;
				}
			}
		}

		public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, bool isIpAddress, string hostName, int portNumber, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
		{
			return TryConnectInner(domain, isIpAddress, hostName, portNumber, string.Empty, string.Empty, languageCode, applicationAssembly, connectedFunc, ConnectOperation.Connect);
		}

		public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, bool isIpAddress, string hostName, int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
		{
			return TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, languageCode, applicationAssembly, connectedFunc, ConnectOperation.ConnectAndCreateAccount);
		}

		public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, bool isIpAddress, string hostName, int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
		{
			return TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, languageCode, applicationAssembly, connectedFunc, ConnectOperation.ConnectAndConnectToAccount);
		}

		private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, bool isIpAddress, string hostName, int portNumber, string userName, string password, string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
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

		public async Task<ContractsClient> CreateContractsClientAsync(bool CanCreateKeys)
		{
			if (this.xmppClient is null)
			{
				throw new InvalidOperationException("XmppClient is not connected");
			}
			if (string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
			{
				throw new InvalidOperationException("LegalJid is not defined");
			}

			ContractsClient Result = new ContractsClient(this.xmppClient, this.tagProfile.LegalJid);

			if (!await Result.LoadKeys(false))
			{
				if (!CanCreateKeys)
				{
					Log.Alert("Regeneration of keys not permitted at this time.",
						string.Empty, string.Empty, string.Empty, EventLevel.Major, string.Empty, string.Empty, Environment.StackTrace);

					throw new Exception("Regeneration of keys not permitted at this time.");
				}

				await Result.GenerateNewKeys();	
			}

			return Result;
		}

		public HttpFileUploadClient CreateFileUploadClient()
		{
			if (this.xmppClient is null)
				throw new InvalidOperationException("The XMPP Client is not connected");

			if (string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid))
				throw new InvalidOperationException("No HTTP File Upload Service defined");

			if (!this.tagProfile.HttpFileUploadMaxSize.HasValue)
				throw new InvalidOperationException("HttpFileUploadMaxSize is not defined");

			return new HttpFileUploadClient(this.xmppClient, this.tagProfile.HttpFileUploadJid, this.tagProfile.HttpFileUploadMaxSize);
		}

		public MultiUserChatClient CreateMultiUserChatClient()
		{
			if (this.xmppClient is null)
				throw new InvalidOperationException("The XMPP Client is not connected");
			
			if (string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
				throw new InvalidOperationException("There is no Multi-User Chat Service defined.");

			return new MultiUserChatClient(this.xmppClient, this.tagProfile.MucJid);
		}

		public ThingRegistryClient CreateThingRegistryClient()
		{
			if (this.xmppClient is null)
				throw new InvalidOperationException("The XMPP Client is not connected");

			if (string.IsNullOrWhiteSpace(this.tagProfile.RegistryJid))
				throw new InvalidOperationException("There is no Thing Registry Service defined.");

			return new ThingRegistryClient(this.xmppClient, this.tagProfile.RegistryJid);
		}

		public EDalerClient CreateEDalerClient()
		{
			if (this.xmppClient is null)
				throw new InvalidOperationException("The XMPP Client is not connected");

			if (string.IsNullOrWhiteSpace(this.tagProfile.EDalerJid))
				throw new InvalidOperationException("There is no eDaler Service defined.");

			return new EDalerClient(this.xmppClient, this.Contracts.ContractsClient, this.tagProfile.EDalerJid);
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
			if (!(xmppClient is null) &&
				(xmppClient.State == XmppState.Error || xmppClient.State == XmppState.Offline) &&
				this.networkService.IsOnline)
			{
				xmppClient.Reconnect();
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
			{
				response = sniffer.SnifferToText();
			}
			else
			{
				response = sniffer.SnifferToText().Replace(historyTextData, "");
			}

			return response;
		}


		public string CommsDumpAsHtml(bool history = false)
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