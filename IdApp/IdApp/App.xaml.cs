using EDaler;
using IdApp.DeviceSpecific;
using IdApp.Extensions;
using IdApp.Helpers.Svg;
using IdApp.Pages;
using IdApp.Pages.Main.Shell;
using IdApp.Popups.Pin.PinPopup;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.AttachmentCache;
using IdApp.Services.Contracts;
using IdApp.Services.Crypto;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Nfc;
using IdApp.Services.Settings;
using IdApp.Services.Storage;
using IdApp.Services.Tag;
using IdApp.Services.ThingRegistries;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using IdApp.Services.Wallet;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.P2P.E2E;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Push;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Waher.Runtime.Settings;
using Waher.Runtime.Text;
using Waher.Security;
using Waher.Security.LoginMonitor;

using Waher.Script;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using System.Globalization;
using System.Linq;
using Xamarin.CommunityToolkit.Helpers;
using Rg.Plugins.Popup.Services;

namespace IdApp
{
	/// <summary>
	/// The Application class, representing an instance of the IdApp.
	/// </summary>
	public partial class App
	{
		private static readonly TaskCompletionSource<bool> servicesSetup = new();
		private static readonly TaskCompletionSource<bool> configLoaded = new();
		private static readonly TaskCompletionSource<bool> defaultInstantiatedSource = new();
		private static bool defaultInstantiated = false;
		private static App instance;
		private static DateTime savedStartTime = DateTime.MinValue;
		private static bool displayedPinPopup = false;
		private readonly LoginAuditor loginAuditor;
		private Timer autoSaveTimer;
		private ServiceReferences services;
		private Profiler startupProfiler;
		private readonly Task<bool> initCompleted;
		private readonly SemaphoreSlim startupWorker = new(1, 1);
		private CancellationTokenSource startupCancellation;

		// The App class is not actually a singleton. Each time Android MainActivity is destroyed and then created again, a new instance
		// of the App class will be created, its OnStart method will be called and its OnResume method will not be called. This happens,
		// for example, on Android when the user presses the back button and then navigates to the app again. However, the App class
		// doesn't seem to work properly (should it?) when this happens (some chaos happens here and there), so we pretend that
		// there is only one instance (see the references to onStartResumesApplication).
		private bool onStartResumesApplication = false;

		///<inheritdoc/>
		public App()
		{
			App PreviousInstance = instance;
			this.onStartResumesApplication = PreviousInstance is not null;
			instance = this;

			// If the previous instance is null, create the app state from scratch. If not, just copy the state from the previous instance.
			if (PreviousInstance is null)
			{
				this.InitLocalizationResource();

				this.startupProfiler = new Profiler("App.ctor", ProfilerThreadType.Sequential);  // Comment out to remove startup profiling.
				this.startupProfiler?.Start();
				this.startupProfiler?.NewState("Init");

				SvgImageSource.RegisterAssembly();

				AppDomain.CurrentDomain.FirstChanceException += this.CurrentDomain_FirstChanceException;
				AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
				TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

				LoginInterval[] LoginIntervals = new[] {
					new LoginInterval(Constants.Pin.MaxPinAttempts, TimeSpan.FromDays(Constants.Pin.FirstBlockInDays)),
					new LoginInterval(Constants.Pin.MaxPinAttempts, TimeSpan.FromDays(Constants.Pin.SecondBlockInDays))};

				this.loginAuditor = new LoginAuditor(Constants.Pin.LogAuditorObjectID, LoginIntervals);
				this.startupCancellation = new CancellationTokenSource();
				this.initCompleted = this.Init();
			}
			else
			{
				this.loginAuditor = PreviousInstance.loginAuditor;
				this.autoSaveTimer = PreviousInstance.autoSaveTimer;
				this.services = PreviousInstance.services;
				this.startupProfiler = PreviousInstance.startupProfiler;
				this.initCompleted = PreviousInstance.initCompleted;
				this.startupWorker = PreviousInstance.startupWorker;
				this.startupCancellation = PreviousInstance.startupCancellation;
			}

			this.InitializeComponent();
			Current.UserAppTheme = OSAppTheme.Unspecified;

			// Start page
			try
			{
				this.startupProfiler?.NewState("MainPage");

				this.MainPage = new AppShell();
			}
			catch (Exception ex)
			{
				this.HandleStartupException(ex);
			}

			this.startupProfiler?.MainThread?.Idle();
		}

		private void InitLocalizationResource()
		{
			List<string> SupportedLanguages = new() { "en", "sv", "es", "fr", "de", "da", "no", "fi", "sr", "pt", "ro", "ru" };

			string SelectedLanguage = Preferences.Get("user_selected_language", null);

			if (SelectedLanguage is null)
			{
				string LanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
				string SupportedLanguage = SupportedLanguages.FirstOrDefault(el => el == LanguageName);
				SelectedLanguage = string.IsNullOrEmpty(SupportedLanguage) ? "en" : LanguageName;

				Preferences.Set("user_selected_language", SelectedLanguage);
			}

			CultureInfo[] Infos = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
			CultureInfo SelectedInfo = Infos.First(el => el.Name == SelectedLanguage);

			LocalizationResourceManager.Current.Init(AppResources.ResourceManager, SelectedInfo);
		}

		private Task<bool> Init()
		{
			ProfilerThread Thread = this.startupProfiler?.CreateThread("Init", ProfilerThreadType.Sequential);
			Thread?.Start();

			TaskCompletionSource<bool> Result = new();
			Task.Run(async () => await this.InitInParallel(Thread, Result));
			return Result.Task;
		}

		private async Task InitInParallel(ProfilerThread Thread, TaskCompletionSource<bool> Result)
		{
			try
			{
				this.InitInstances(Thread);

				// Get the db started right away to save startup time.

				Thread?.NewState("DB");
				ProfilerThread SubThread = Thread?.CreateSubThread("Database", ProfilerThreadType.Sequential);

				await this.startupWorker.WaitAsync();

				try
				{
					await this.services.StorageService.Init(SubThread, null);

					Thread?.NewState("Config");
					await this.CreateOrRestoreConfiguration();
					configLoaded.TrySetResult(true);
				}
				finally
				{
					this.startupWorker.Release();
				}

				await this.PerformStartup(false, Thread);

				Result.TrySetResult(true);
			}
			catch (Exception ex)
			{
				ex = Waher.Events.Log.UnnestException(ex);
				Thread?.Exception(ex);
				this.HandleStartupException(ex);

				servicesSetup.TrySetResult(false);
				Result.TrySetResult(false);
			}

			Thread?.Stop();
		}

		private void InitInstances(ProfilerThread Thread)
		{
			Thread?.NewState("Types");

			Assembly appAssembly = this.GetType().Assembly;

			if (!Types.IsInitialized)
			{
				// Define the scope and reach of Runtime.Inventory (Script, Serialization, Persistence, IoC, etc.):
				Types.Initialize(
					appAssembly,                                // Allows for objects defined in this assembly, to be instantiated and persisted.
					typeof(Database).Assembly,                  // Indexes default attributes
					typeof(ObjectSerializer).Assembly,          // Indexes general serializers
					typeof(FilesProvider).Assembly,             // Indexes special serializers
					typeof(RuntimeSettings).Assembly,           // Allows for persistence of settings in the object database
					typeof(InternetContent).Assembly,           // Common Content-Types
					typeof(ImageCodec).Assembly,                // Common Image Content-Types
					typeof(XML).Assembly,                       // XML Content-Type
					typeof(MarkdownDocument).Assembly,          // Markdown support
					typeof(DnsResolver).Assembly,               // Serialization of DNS-related objects
					typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
					typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
					typeof(ProvisioningClient).Assembly,        // Serialization of XMPP objects related to thing registries, provisioning and decision support.
					typeof(SensorClient).Assembly,              // Serialization of XMPP objects related to sensors
					typeof(ControlClient).Assembly,             // Serialization of XMPP objects related to actuators
					typeof(ConcentratorClient).Assembly,        // Serialization of XMPP objects related to concentrators
					typeof(Expression).Assembly,                // Indexes basic script functions
					typeof(EDalerClient).Assembly,              // Indexes eDaler client framework
					typeof(NeuroFeaturesClient).Assembly,       // Indexes Neuro-Features client framework
					typeof(PushNotificationClient).Assembly,    // Indexes Push Notification client framework
					typeof(XmppServerlessMessaging).Assembly);  // Indexes End-to-End encryption mechanisms
			}

			EndpointSecurity.SetCiphers(new Type[]
			{
				typeof(Edwards448Endpoint)
			}, false);

			Thread?.NewState("SDK");

			// Create Services

			Types.InstantiateDefault<ITagProfile>(false);
			Types.InstantiateDefault<ILogService>(false);
			Types.InstantiateDefault<IUiSerializer>(false);
			Types.InstantiateDefault<ICryptoService>(false);
			Types.InstantiateDefault<INetworkService>(false);
			Types.InstantiateDefault<IStorageService>(false);
			Types.InstantiateDefault<ISettingsService>(false);
			Types.InstantiateDefault<INavigationService>(false);
			Types.InstantiateDefault<IXmppService>(false, appAssembly, this.startupProfiler);
			Types.InstantiateDefault<IAttachmentCacheService>(false);
			Types.InstantiateDefault<IContractOrchestratorService>(false);
			Types.InstantiateDefault<IThingRegistryOrchestratorService>(false);
			Types.InstantiateDefault<INeuroWalletOrchestratorService>(false);
			Types.InstantiateDefault<INfcService>(false);

			this.services = new ServiceReferences();

			defaultInstantiatedSource.TrySetResult(true);
			defaultInstantiated = true;

			// Set resolver

			DependencyResolver.ResolveUsing(type =>
			{
				if (Types.GetType(type.FullName) is null)
					return null;    // Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.

				return Types.Instantiate(true, type);
			});

			servicesSetup.TrySetResult(true);
		}

		private void HandleStartupException(Exception ex)
		{
			ex = Waher.Events.Log.UnnestException(ex);
			this.startupProfiler?.Exception(ex);
			this.services?.LogService?.SaveExceptionDump("StartPage", ex.ToString());
			this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
			return;
		}

		/// <summary>
		/// Instantiates an object of type <typeparamref name="T"/>, after assuring default instances have been created first.
		/// ASsures singleton classes are only instantiated once, and that the reference to the singleton instance is returned.
		/// </summary>
		/// <typeparam name="T">Type of object to instantiate.</typeparam>
		/// <returns>Instance</returns>
		public static T Instantiate<T>()
		{
			if (!defaultInstantiated)
				defaultInstantiated = defaultInstantiatedSource.Task.Result;

			return Types.Instantiate<T>(false);
		}

		internal static async Task WaitForServiceSetup()
		{
			await servicesSetup.Task;
		}

		internal static async Task WaitForConfigLoaded()
		{
			await configLoaded.Task;
		}

		#region Startup/Shutdown

		///<inheritdoc/>
		protected override async void OnStart()
		{
			if (this.onStartResumesApplication)
			{
				this.onStartResumesApplication = false;
				this.OnResume();
				return;
			}

			if (!this.initCompleted.Wait(60000))
				throw new Exception("Initialization did not complete in time.");

			this.StartupCompleted("StartupProfile.uml", false);

			if (!await App.VerifyPin())
				await App.Stop();
		}

		///<inheritdoc/>
		protected override async void OnResume()
		{
			instance = this;
			this.startupCancellation = new CancellationTokenSource();

			await this.PerformStartup(true, null);

			if (!await App.VerifyPin())
				await App.Stop();
		}


		private async Task PerformStartup(bool isResuming, ProfilerThread Thread)
		{
			await this.startupWorker.WaitAsync();

			try
			{
				// cancel the startup if the application is closed
				CancellationToken Token = this.startupCancellation.Token;
				Token.ThrowIfCancellationRequested();

				Thread?.NewState("Report");
				await this.SendErrorReportFromPreviousRun();

				Thread?.NewState("Startup");
				this.services.UiSerializer.IsRunningInTheBackground = false;

				Token.ThrowIfCancellationRequested();

				// Start the db.
				// This is for soft restarts.
				// If this is a cold start, this call is made already in the App ctor, and this is then a no-op.

				Thread?.NewState("DB");
				await this.services.StorageService.Init(Thread, Token);

				if (!isResuming)
					await WaitForConfigLoaded();

				Thread?.NewState("Network");
				await this.services.NetworkService.Load(isResuming, Token);

				Thread?.NewState("XMPP");
				await this.services.XmppService.Load(isResuming, Token);

				Thread?.NewState("Timer");
				TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
				this.autoSaveTimer = new Timer(async _ => await this.AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

				Thread?.NewState("Navigation");
				await this.services.NavigationService.Load(isResuming, Token);

				Thread?.NewState("Cache");
				await this.services.AttachmentCacheService.Load(isResuming, Token);

				Thread?.NewState("Orchestrators");
				await this.services.ContractOrchestratorService.Load(isResuming, Token);
				await this.services.ThingRegistryOrchestratorService.Load(isResuming, Token);
			}
			catch (OperationCanceledException)
			{
				Waher.Events.Log.Notice($"{(isResuming ? "OnResume " : "Initial app ")} startup was canceled.");
			}
			catch (Exception ex)
			{
				ex = Waher.Events.Log.UnnestException(ex);
				Thread?.Exception(ex);
				this.services?.LogService?.SaveExceptionDump(ex.Message, ex.StackTrace);
				this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
			}
			finally
			{
				Thread?.Stop();
				this.startupWorker.Release();
			}
		}

		///<inheritdoc/>
		protected override async void OnSleep()
		{
			// Done manually here, as the Disappearing event won't trigger when exiting the app,
			// and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.

			if (this.MainPage?.BindingContext is BaseViewModel vm)
				await vm.Shutdown();

			await this.Shutdown(false);

			this.SetStartInactivityTime();
		}

		internal static async Task Stop()
		{
			if (instance is not null)
			{
				await instance.Shutdown(false);
				instance = null;
			}

			ICloseApplication closeApp = DependencyService.Get<ICloseApplication>();
			if (closeApp is not null)
				await closeApp.Close();
			else
				Environment.Exit(0);
		}

		private async Task Shutdown(bool inPanic)
		{
			// if the PerformStartup is not finished, cancel it first
			this.startupCancellation.Cancel();
			await this.startupWorker.WaitAsync();

			try
			{
				this.StopAutoSaveTimer();

				if (this.services?.UiSerializer is not null)
					this.services.UiSerializer.IsRunningInTheBackground = !inPanic;

				if (inPanic)
				{
					if (this.services?.XmppService is not null)
						await this.services.XmppService.UnloadFast();
				}
				else
				{
					if (this.services?.NavigationService is not null)
						await this.services.NavigationService.Unload();

					if (this.services.ContractOrchestratorService is not null)
						await this.services.ContractOrchestratorService.Unload();

					if (this.services.ThingRegistryOrchestratorService is not null)
						await this.services.ThingRegistryOrchestratorService.Unload();

					if (this.services?.XmppService is not null)
						await this.services.XmppService.Unload();

					if (this.services?.NetworkService is not null)
						await this.services.NetworkService.Unload();

					if (this.services.AttachmentCacheService is not null)
						await this.services.AttachmentCacheService.Unload();
				}

				foreach (IEventSink Sink in Waher.Events.Log.Sinks)
					Waher.Events.Log.Unregister(Sink);

				if (this.services?.StorageService is not null)
					await this.services.StorageService.Shutdown();

				// Causes list of singleton instances to be cleared.
				Waher.Events.Log.Terminate();
			}
			finally
			{
				this.startupWorker.Release();
			}
		}

		#endregion

		private void StopAutoSaveTimer()
		{
			if (this.autoSaveTimer is not null)
			{
				this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
				this.autoSaveTimer.Dispose();
				this.autoSaveTimer = null;
			}
		}

		private async Task AutoSave()
		{
			if (this.services.TagProfile.IsDirty)
			{
				this.services.TagProfile.ResetIsDirty();
				try
				{
					TagConfiguration tc = this.services.TagProfile.ToConfiguration();

					try
					{
						if (string.IsNullOrEmpty(tc.ObjectId))
							await this.services.StorageService.Insert(tc);
						else
							await this.services.StorageService.Update(tc);
					}
					catch (KeyNotFoundException)
					{
						await this.services.StorageService.Insert(tc);
					}
				}
				catch (Exception ex)
				{
					this.services.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}
		}

		private async Task CreateOrRestoreConfiguration()
		{
			TagConfiguration configuration;

			try
			{
				configuration = await this.services.StorageService.FindFirstDeleteRest<TagConfiguration>();
			}
			catch (Exception findException)
			{
				this.services.LogService.LogException(findException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				configuration = null;
			}

			if (configuration is null)
			{
				configuration = new TagConfiguration();

				try
				{
					await this.services.StorageService.Insert(configuration);
				}
				catch (Exception insertException)
				{
					this.services.LogService.LogException(insertException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			this.services.TagProfile.FromConfiguration(configuration);
		}

		#region Error Handling

		private async void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			Exception ex = e.Exception;
			e.SetObserved();

			ex = Waher.Events.Log.UnnestException(ex);

			await this.Handle_UnhandledException(ex, nameof(TaskScheduler_UnobservedTaskException), false);
		}

		private async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			await this.Handle_UnhandledException(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), true);
		}

		private async Task Handle_UnhandledException(Exception ex, string title, bool shutdown)
		{
			if (ex is not null)
			{
				this.services?.LogService?.SaveExceptionDump(title, ex.ToString());
				this.services?.LogService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
			}

			if (shutdown)
				await this.Shutdown(false);

#if DEBUG
			if (!shutdown)
			{
				if (Device.IsInvokeRequired && (this.MainPage is not null))
					Device.BeginInvokeOnMainThread(async () => await this.MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok));
				else if (this.MainPage is not null)
					await this.MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok);
			}
#endif
		}

		private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
		{
			this.startupProfiler?.Exception(e.Exception);
		}

		private void DisplayBootstrapErrorPage(string Title, string StackTrace)
		{
			this.Dispatcher.BeginInvokeOnMainThread(() =>
			{
				this.MainPage = new BootstrapErrorPage
				{
					BindingContext = new BootstrapErrorViewModel
					{
						Title = Title,
						StackTrace = StackTrace,
					},
				};
			});
		}

		private async Task SendErrorReportFromPreviousRun()
		{
			if (this.services?.LogService is not null)
			{
				string stackTrace = this.services.LogService.LoadExceptionDump();
				if (!string.IsNullOrWhiteSpace(stackTrace))
				{
					try
					{
						await SendAlert(stackTrace, "text/plain");
					}
					finally
					{
						this.services.LogService.DeleteExceptionDump();
					}
				}
			}
		}

		internal static async Task SendAlert(string message, string contentType)
		{
			try
			{
				HttpClient client = new();
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				StringContent content = new(message);
				content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

				await client.PostAsync("https://lab.tagroot.io/Alert.ws", content);
			}
			catch (Exception ex)
			{
				Waher.Events.Log.Critical(ex);
			}
		}

		private void StartupCompleted(string ProfileFileName, bool SendProfilingAsAlert)
		{
			AppDomain.CurrentDomain.FirstChanceException -= this.CurrentDomain_FirstChanceException;

			if (this.startupProfiler is not null)
			{
				this.startupProfiler.Stop();

				string uml = this.startupProfiler.ExportPlantUml(TimeUnit.MilliSeconds);

				try
				{
					string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					if (!Directory.Exists(AppDataFolder))
						Directory.CreateDirectory(AppDataFolder);

					ProfileFileName = Path.Combine(AppDataFolder, ProfileFileName);
					File.WriteAllText(ProfileFileName, uml);
				}
				catch (Exception)
				{
					// Ignore, if not able to save file.
				}

				if (SendProfilingAsAlert)
				{
					Task.Run(async () =>
					{
						try
						{
							await SendAlert("```uml\r\n" + uml + "```", "text/markdown");
						}
						catch (Exception ex)
						{
							Waher.Events.Log.Critical(ex);
						}
					});
				}

				this.startupProfiler = null;
			}
		}

		/// <summary>
		/// Sends the contents of the database to support.
		/// </summary>
		/// <param name="FileName">Name of file used to keep track of state changes.</param>
		/// <param name="IncludeUnchanged">If unchanged material should be included.</param>
		/// <param name="SendAsAlert">If an alert is to be sent.</param>
		public static async Task EvaluateDatabaseDiff(string FileName, bool IncludeUnchanged, bool SendAsAlert)
		{
			StringBuilder Xml = new();

			using (XmlDatabaseExport Output = new(Xml, true, 256))
			{
				await Database.Export(Output);
			}

			string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileName = Path.Combine(AppDataFolder, FileName);

			string CurrentState = Xml.ToString();
			string PrevState = File.Exists(FileName) ? File.ReadAllText(FileName) : string.Empty;

			EditScript<string> Script = Difference.AnalyzeRows(PrevState, CurrentState);
			StringBuilder Markdown = new();
			string Prefix;

			Markdown.Append("Database content changes (`");
			Markdown.Append(FileName);
			Markdown.AppendLine("`):");

			foreach (Step<string> Step in Script.Steps)
			{
				Markdown.AppendLine();

				switch (Step.Operation)
				{
					case EditOperation.Keep:
					default:
						if (!IncludeUnchanged)
							continue;

						Prefix = ">\t";
						break;

					case EditOperation.Insert:
						Prefix = "+>\t";
						break;

					case EditOperation.Delete:
						Prefix = "->\t";
						break;
				}

				Markdown.Append(Prefix);
				Markdown.AppendLine("```xml");

				foreach (string Row in Step.Symbols)
				{
					Markdown.Append(Prefix);
					Markdown.Append(Row);
					Markdown.AppendLine("  ");
				}

				Markdown.Append(Prefix);
				Markdown.AppendLine("```");
			}

			string DiffMsg = Markdown.ToString();

			if (SendAsAlert)
				await SendAlert(DiffMsg, "text/markdown");

			File.WriteAllText(FileName, CurrentState);
			File.WriteAllText(FileName + ".diff.md", DiffMsg);
		}

		#endregion

		/// <summary>
		/// Opens an URL in the application.
		/// </summary>
		/// <param name="Url">URL</param>
		/// <returns>If URL is processed or not.</returns>
		public static Task<bool> OpenUrl(string Url)
		{
			return QrCode.OpenUrl(Url);
		}

		/// <summary>
		/// Asks the user to input its PIN. PIN is verified before being returned.
		/// </summary>
		/// <returns>PIN, if the user has provided the correct PIN. Empty string, if PIN is not configured, null if operation is cancelled.</returns>
		public static async Task<string> InputPin()
		{
			ITagProfile Profile = App.Instantiate<ITagProfile>();
			if (!Profile.UsePin)
				return string.Empty;

			return await InputPin(Profile);
		}

		/// <summary>
		/// Asks the user to verify with its PIN.
		/// </summary>
		/// <returns>If the user has provided the correct PIN</returns>
		public static async Task<bool> VerifyPin()
		{
//#if !DEBUG
			ITagProfile Profile = App.Instantiate<ITagProfile>();
			if (!Profile.UsePin)
				return true;

			bool NeedToVerifyPin = IsInactivitySafeIntervalPassed();

			if (!displayedPinPopup && NeedToVerifyPin)
				return await InputPin(Profile) is not null;
//#endif
			//await instance.loginAuditor.UnblockAndReset(Constants.Pin.RemoteEndpoint);
			return true;
		}

		public static async Task CheckUserLock()
		{
			IUiSerializer Ui = null;
			if (Ui is null)
				Ui = Instantiate<IUiSerializer>();
			DateTime? DateTimeForLogin = await instance.loginAuditor.GetEarliestLoginOpportunity(Constants.Pin.RemoteEndpoint,
								Constants.Pin.Protocol);

			if (DateTimeForLogin.HasValue)
			{
				string MessageAlert;

				if (DateTimeForLogin == DateTime.MaxValue)
				{
					MessageAlert = AppResources.PinIsInvalidAplicationBlockedForever;
					await Ui.DisplayAlert(AppResources.ErrorTitle, MessageAlert);
					await Stop();
				}
				else
				{
					MessageAlert = string.Format(AppResources.PinIsInvalidAplicationBlocked, DateTimeForLogin);
					await Ui.DisplayAlert(AppResources.ErrorTitle, MessageAlert);
					await Stop();
				}
			}
		}

		public static async Task<string> CheckPin(string Pin, ITagProfile Profile)
		{
			if (Pin is null)
				return null;
			long PinAttemptCounter = await GetCurrentPinCounter();
			IUiSerializer Ui = null;
			if (Ui is null)
				Ui = App.Instantiate<IUiSerializer>();
			if (Profile.ComputePinHash(Pin) == Profile.PinHash)
			{
				ClearStartInactivityTime();
				SetCurrentPinCounter(0);
				await instance.loginAuditor.UnblockAndReset(Constants.Pin.RemoteEndpoint);
				await PopupNavigation.Instance.PopAsync();
				return Pin;
			}
			else
			{
				await instance.loginAuditor.ProcessLoginFailure(Constants.Pin.RemoteEndpoint,
						Constants.Pin.Protocol, DateTime.Now, Constants.Pin.Reason);

				PinAttemptCounter++;
				SetCurrentPinCounter(PinAttemptCounter);
			}

			long RemainingAttempts = Constants.Pin.MaxPinAttempts - PinAttemptCounter;

			await Ui.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.PinIsInvalid, RemainingAttempts));
			await CheckUserLock();
			return Pin;
		}

		private static async Task<string> InputPin(ITagProfile Profile)
		{
			displayedPinPopup = true;

			try
			{
				if (!Profile.UsePin)
					return string.Empty;

				PinPopupPage Page = new();
				await PopupNavigation.Instance.PushAsync(Page);
				await CheckUserLock();
				string Pin = await Page.Result;
				return await CheckPin(Pin, Profile);
					
			}
			finally
			{
				displayedPinPopup = false;
			}
		}

		/// <summary>
		/// Set start time of inactivity
		/// </summary>
		private void SetStartInactivityTime()
		{
			savedStartTime = DateTime.Now;
		}

		/// <summary>
		/// Clears the conditions of checking inactivity
		/// </summary>
		private static void ClearStartInactivityTime()
		{
			savedStartTime = DateTime.MaxValue;
		}

		/// <summary>
		/// Performs a check whether 5 minutes of inactivity interval has been passed
		/// </summary>
		/// <returns>True if 5 minutes has been passed and False if has not been passed</returns>
		private static bool IsInactivitySafeIntervalPassed()
		{
			return DateTime.Now.Subtract(savedStartTime).TotalMinutes
				> Constants.Pin.PossibleInactivityInMinutes;
		}

		/// <summary>
		/// Obtains the value for CurrentPinCounter
		/// </summary>
		private static async Task<long> GetCurrentPinCounter()
		{
			return await instance.services.SettingsService.RestoreLongState(Constants.Pin.CurrentPinAttemptCounter);
		}

		/// <summary>
		/// Saves that the value for CurrentPinCounter
		/// </summary>
		private static async void SetCurrentPinCounter(long CurrentPinAttemptCounter)
		{
			await instance.services.SettingsService.SaveState(Constants.Pin.CurrentPinAttemptCounter, CurrentPinAttemptCounter);
		}
	}
}
