﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EDaler;
using IdApp.DeviceSpecific;
using IdApp.Extensions;
using IdApp.Pages;
using IdApp.Pages.Main.Shell;
using IdApp.Popups.Pin.PinPopup;
using IdApp.Services;
using IdApp.Services.AttachmentCache;
using IdApp.Services.Contracts;
using IdApp.Services.ThingRegistries;
using IdApp.Services.Wallet;
using IdApp.Services.UI.QR;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using IdApp.Services.EventLog;
using IdApp.Services.Crypto;
using IdApp.Services.Network;
using IdApp.Services.Storage;
using IdApp.Services.Settings;
using IdApp.Services.Navigation;
using IdApp.Services.Xmpp;
using IdApp.Services.Nfc;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Xml;
using Waher.Content.Markdown;
using Waher.Events;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.P2P.E2E;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Waher.Runtime.Settings;
using Waher.Runtime.Text;
using Waher.Script;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using IdApp.Resx;
using NeuroFeatures;
using Waher.Networking.XMPP.Push;

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
		private Timer autoSaveTimer;
		private ServiceReferences services;
		private Profiler startupProfiler;
		private readonly Task<bool> initCompleted;
		private readonly SemaphoreSlim startupWorker = new(1, 1);
		private CancellationTokenSource startupCancellation = new();

		///<inheritdoc/>
		public App()
		{
			this.startupProfiler = new Profiler("App.ctor", ProfilerThreadType.Sequential);  // Comment out to remove startup profiling.
			this.startupProfiler?.Start();
			this.startupProfiler?.NewState("Init");

			AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			this.initCompleted = this.Init();

			InitializeComponent();

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
				InitInstances(Thread);

				// Get the db started right away to save startup time.

				Thread?.NewState("DB");
				ProfilerThread SubThread = Thread?.CreateSubThread("Database", ProfilerThreadType.Sequential);

				this.startupWorker.Wait();

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
			Types.InstantiateDefault<IXmppService>(false, appAssembly, startupProfiler);
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
			DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
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
		protected override void OnStart()
		{
			instance = this;

			if (this.startupCancellation is null)
				this.startupCancellation = new CancellationTokenSource();

			if (!this.initCompleted.Wait(60000))
				throw new Exception("Initialization did not complete in time.");

			this.StartupCompleted("StartupProfile.uml", false);
		}

		///<inheritdoc/>
		protected override async void OnResume()
		{
			instance = this;

			if (this.startupCancellation is null)
				this.startupCancellation = new CancellationTokenSource();

			await this.PerformStartup(true, null);
		}

		private async Task PerformStartup(bool isResuming, ProfilerThread Thread)
		{
			this.startupWorker.Wait();

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
				this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

				Thread?.NewState("Navigation");
				await this.services.NavigationService.Load(isResuming, Token);

				Thread?.NewState("Cache");
				await this.services.AttachmentCacheService.Load(isResuming, Token);

				Thread?.NewState("Orchestrators");
				await this.services.ContractOrchestratorService.Load(isResuming, Token);
				await this.services.ThingRegistryOrchestratorService.Load(isResuming, Token);
			}
			catch (Exception ex)
			{
				ex = Waher.Events.Log.UnnestException(ex);
				Thread?.Exception(ex);
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

			if (MainPage?.BindingContext is BaseViewModel vm)
				await vm.Shutdown();

			await this.Shutdown(false);
		}

		internal static async Task Stop()
		{
			if (instance is not null)
			{
				await instance.Shutdown(false);
				instance = null;
			}

			ICloseApplication closeApp = DependencyService.Get<ICloseApplication>();
			if (!(closeApp is null))
				await closeApp.Close();
			else
				Environment.Exit(0);
		}

		private async Task Shutdown(bool inPanic)
		{
			// if the PerformStartup is not finished, cancel it first
			this.startupCancellation.Cancel();
			this.startupWorker.Wait();

			try
			{
				StopAutoSaveTimer();

				if (!(this.services?.UiSerializer is null))
					this.services.UiSerializer.IsRunningInTheBackground = !inPanic;

				if (inPanic)
				{
					if (!(this.services?.XmppService is null))
						await this.services.XmppService.UnloadFast();
				}
				else
				{
					if (!(this.services?.NavigationService is null))
						await this.services.NavigationService.Unload();

					if (!(this.services.ContractOrchestratorService is null))
						await this.services.ContractOrchestratorService.Unload();

					if (!(this.services.ThingRegistryOrchestratorService is null))
						await this.services.ThingRegistryOrchestratorService.Unload();

					if (!(this.services?.XmppService is null))
						await this.services.XmppService.Unload();

					if (!(this.services?.NetworkService is null))
						await this.services.NetworkService.Unload();

					if (!(this.services.AttachmentCacheService is null))
						await this.services.AttachmentCacheService.Unload();
				}

				foreach (IEventSink Sink in Waher.Events.Log.Sinks)
					Waher.Events.Log.Unregister(Sink);

				if (!(this.services?.StorageService is null))
					await this.services.StorageService.Shutdown();

				// Causes list of singleton instances to be cleared.
				Waher.Events.Log.Terminate();
			}
			finally
			{
				this.startupWorker.Release();
				this.startupCancellation = null;
			}
		}

		#endregion

		private void StopAutoSaveTimer()
		{
			if (!(this.autoSaveTimer is null))
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

			await Handle_UnhandledException(ex, nameof(TaskScheduler_UnobservedTaskException), false);
		}

		private async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			await Handle_UnhandledException(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), true);
		}

		private async Task Handle_UnhandledException(Exception ex, string title, bool shutdown)
		{
			if (!(ex is null))
				this.services?.LogService?.SaveExceptionDump(title, ex.ToString());

			if (!(ex is null))
				this.services?.LogService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));

			if (shutdown)
				await this.Shutdown(false);

#if DEBUG
			if (!shutdown)
			{
				if (Device.IsInvokeRequired && !(MainPage is null))
					Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok));
				else if (!(MainPage is null))
					await MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok);
			}
#endif
		}

		private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
		{
			this.startupProfiler?.Exception(e.Exception);
		}

		private void DisplayBootstrapErrorPage(string title, string stackTrace)
		{
			Dispatcher.BeginInvokeOnMainThread(() =>
			{
				this.services?.LogService?.SaveExceptionDump(title, stackTrace);

				ScrollView sv = new();
				StackLayout sl = new()
				{
					Orientation = StackOrientation.Vertical,
				};

				sl.Children.Add(new Label
				{
					Text = title,
					FontSize = 24,
					HorizontalOptions = LayoutOptions.FillAndExpand,
				});

				sl.Children.Add(new Label
				{
					Text = stackTrace,
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand
				});

				Button b = new() { Text = "Copy to clipboard", Margin = 12 };
				b.Clicked += async (sender, args) => await Clipboard.SetTextAsync(stackTrace);
				sl.Children.Add(b);

				sv.Content = sl;

				this.MainPage = new ContentPage
				{
					Content = sv
				};
			});
		}

		private async Task SendErrorReportFromPreviousRun()
		{
			if (!(this.services?.LogService is null))
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
			AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;

			if (!(this.startupProfiler is null))
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

			IUiSerializer Ui = null;

			while (true)
			{
				PinPopupPage Page = new();

				await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(Page);
				string Pin = await Page.Result;

				if (Pin is null)
					return null;

				if (Profile.ComputePinHash(Pin) == Profile.PinHash)
					return Pin;

				if (Ui is null)
					Ui = App.Instantiate<IUiSerializer>();

				await Ui.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);

				// TODO: Limit number of attempts.
			}
		}

		/// <summary>
		/// Asks the user to verify with its PIN.
		/// </summary>
		/// <returns>If the user has provided the correct PIN</returns>
		public static async Task<bool> VerifyPin()
		{
			return (!(await InputPin() is null));
		}

	}
}
