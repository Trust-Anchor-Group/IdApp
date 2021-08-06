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
using EDaler;
using IdApp.Pages.Main.Shell;
using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
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
using Device = Xamarin.Forms.Device;

namespace IdApp
{
	/// <summary>
	/// The Application class, representing an instance of the IdApp.
	/// </summary>
	public partial class App
	{
		private static readonly TaskCompletionSource<bool> servicesSetup = new TaskCompletionSource<bool>();
		private static readonly TaskCompletionSource<bool> configLoaded = new TaskCompletionSource<bool>();
		private static readonly TaskCompletionSource<bool> defaultInstantiatedSource = new TaskCompletionSource<bool>();
		private static bool defaultInstantiated = false;
		private static App instance;
		private Timer autoSaveTimer;
		private ITagIdSdk sdk;
		private IAttachmentCacheService attachmentCacheService;
		private IContractOrchestratorService contractOrchestratorService;
		private IThingRegistryOrchestratorService thingRegistryOrchestratorService;
		private IEDalerOrchestratorService eDalerOrchestratorService;
		private Profiler startupProfiler;
		private readonly Task<bool> initCompleted;

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
			TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();

			Task.Run(async () =>
			{
				ProfilerThread Thread = this.startupProfiler?.CreateThread("Init", ProfilerThreadType.Sequential);

				try
				{
					try
					{
						Thread?.Start();
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
								typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
								typeof(TagConfiguration).Assembly);         // Indexes persistable objects
						}

						EndpointSecurity.SetCiphers(new Type[]
						{
							typeof(Edwards448Endpoint)
						}, false);

						Thread?.NewState("SDK");

						// Create Services

						this.sdk = Types.InstantiateDefault<ITagIdSdk>(false, appAssembly, this.startupProfiler);

						this.attachmentCacheService = Types.InstantiateDefault<IAttachmentCacheService>(false, this.sdk.LogService);
						this.contractOrchestratorService = Types.InstantiateDefault<IContractOrchestratorService>(false, this.sdk.TagProfile, this.sdk.UiDispatcher, this.sdk.NeuronService, this.sdk.NavigationService, this.sdk.LogService, this.sdk.NetworkService, this.sdk.SettingsService);
						this.thingRegistryOrchestratorService = Types.InstantiateDefault<IThingRegistryOrchestratorService>(false, this.sdk.TagProfile, this.sdk.UiDispatcher, this.sdk.NeuronService, this.sdk.NavigationService, this.sdk.LogService, this.sdk.NetworkService);
						this.eDalerOrchestratorService = Types.InstantiateDefault<IEDalerOrchestratorService>(false, this.sdk.TagProfile, this.sdk.UiDispatcher, this.sdk.NeuronService, this.sdk.NavigationService, this.sdk.LogService, this.sdk.NetworkService, this.sdk.SettingsService);

						defaultInstantiatedSource.TrySetResult(true);

						// Set resolver

						DependencyResolver.ResolveUsing(type =>
						{
							if (Types.GetType(type.FullName) is null)
								return null;    // Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.

							bool IsReg = SingletonAttribute.IsRegistered(type);
							return Types.Instantiate(true, type);
						});

						servicesSetup.TrySetResult(true);

						// Get the db started right away to save startup time.

						Thread?.NewState("DB");
						await this.sdk.StorageService.Init(Thread?.CreateSubThread("Database", ProfilerThreadType.Sequential));

						Thread?.NewState("Config");
						await this.CreateOrRestoreConfiguration();

						configLoaded.TrySetResult(true);

						await this.PerformStartup(false, Thread);
					}
					catch (Exception ex)
					{
						servicesSetup.TrySetResult(false);
						configLoaded.TrySetResult(false);
						this.HandleStartupException(ex);
						return;
					}
				}
				catch (Exception ex)
				{
					ex = Waher.Events.Log.UnnestException(ex);
					Thread?.Exception(ex);
					this.HandleStartupException(ex);
					Result.TrySetResult(false);
				}
				finally
				{
					Thread?.Stop();
					Result.TrySetResult(true);
				}
			});

			return Result.Task;
		}

		private void HandleStartupException(Exception ex)
		{
			ex = Waher.Events.Log.UnnestException(ex);
			this.startupProfiler?.Exception(ex);
			this.sdk?.LogService?.SaveExceptionDump("StartPage", ex.ToString());
			DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
			return;
		}

		internal static T Instantiate<T>()
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
			await this.initCompleted;
			this.StartupCompleted("StartupProfile.uml", false);
		}

		///<inheritdoc/>
		protected override async void OnResume()
		{
			await this.PerformStartup(true, null);
		}

		private async Task PerformStartup(bool isResuming, ProfilerThread Thread)
		{
			try
			{
				instance = this;

				Thread?.NewState("Report");
				await this.SendErrorReportFromPreviousRun();

				Thread?.NewState("Startup");
				this.sdk.UiDispatcher.IsRunningInTheBackground = false;

				// Start the db.
				// This is for soft restarts.
				// If this is a cold start, this call is made already in the App ctor, and this is then a no-op.

				Thread?.NewState("DB");
				await this.sdk.StorageService.Init(Thread);

				Thread?.NewState("Network");
				await this.sdk.NetworkService.Load(isResuming);

				if (!isResuming)
					await WaitForConfigLoaded();

				Thread?.NewState("Neuron");
				await this.sdk.NeuronService.Load(isResuming);

				Thread?.NewState("Timer");
				TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
				this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

				Thread?.NewState("Navigation");
				await this.sdk.NavigationService.Load(isResuming);

				Thread?.NewState("Cache");
				await this.attachmentCacheService.Load(isResuming);

				Thread?.NewState("Orchestrators");
				await this.contractOrchestratorService.Load(isResuming);
				await this.thingRegistryOrchestratorService.Load(isResuming);
			}
			catch (Exception ex)
			{
				ex = Waher.Events.Log.UnnestException(ex);
				Thread?.Exception(ex);
				this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
			}

			Thread?.Stop();
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
			if (!(instance is null))
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
			StopAutoSaveTimer();

			if (!(this.sdk?.UiDispatcher is null))
				this.sdk.UiDispatcher.IsRunningInTheBackground = !inPanic;

			if (inPanic)
			{
				if (!(this.sdk?.NeuronService is null))
					await this.sdk.NeuronService.UnloadFast();
			}
			else
			{
				if (!(this.sdk?.NavigationService is null))
					await this.sdk.NavigationService.Unload();

				if (!(this.contractOrchestratorService is null))
					await this.contractOrchestratorService.Unload();

				if (!(this.thingRegistryOrchestratorService is null))
					await this.thingRegistryOrchestratorService.Unload();

				if (!(this.sdk?.NeuronService is null))
					await this.sdk.NeuronService.Unload();

				if (!(this.sdk?.NetworkService is null))
					await this.sdk.NetworkService.Unload();

				if (!(this.attachmentCacheService is null))
					await this.attachmentCacheService.Unload();
			}

			foreach (IEventSink Sink in Waher.Events.Log.Sinks)
				Waher.Events.Log.Unregister(Sink);

			if (!(this.sdk?.StorageService is null))
				await this.sdk.StorageService.Shutdown();

			Waher.Events.Log.Terminate();	// Causes list of singleton instances to be cleared.
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
			if (this.sdk.TagProfile.IsDirty)
			{
				this.sdk.TagProfile.ResetIsDirty();
				try
				{
					TagConfiguration tc = this.sdk.TagProfile.ToConfiguration();

					try
					{
						if (string.IsNullOrEmpty(tc.ObjectId))
							await this.sdk.StorageService.Insert(tc);
						else
							await this.sdk.StorageService.Update(tc);
					}
					catch (KeyNotFoundException)
					{
						await this.sdk.StorageService.Insert(tc);
					}
				}
				catch (Exception ex)
				{
					this.sdk.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}
		}

		private async Task CreateOrRestoreConfiguration()
		{
			TagConfiguration configuration;

			try
			{
				configuration = await this.sdk.StorageService.FindFirstDeleteRest<TagConfiguration>();
			}
			catch (Exception findException)
			{
				this.sdk.LogService.LogException(findException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				configuration = null;
			}

			if (configuration is null)
			{
				configuration = new TagConfiguration();

				try
				{
					await this.sdk.StorageService.Insert(configuration);
				}
				catch (Exception insertException)
				{
					this.sdk.LogService.LogException(insertException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			this.sdk.TagProfile.FromConfiguration(configuration);
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
				this.sdk?.LogService?.SaveExceptionDump(title, ex.ToString());

			if (!(ex is null))
				this.sdk?.LogService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));

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
				this.sdk?.LogService?.SaveExceptionDump(title, stackTrace);

				ScrollView sv = new ScrollView();
				StackLayout sl = new StackLayout
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

				Button b = new Button { Text = "Copy to clipboard", Margin = 12 };
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
			if (!(this.sdk?.LogService is null))
			{
				string stackTrace = this.sdk.LogService.LoadExceptionDump();
				if (!string.IsNullOrWhiteSpace(stackTrace))
				{
					try
					{
						await SendAlert(stackTrace, "text/plain");
					}
					finally
					{
						this.sdk.LogService.DeleteExceptionDump();
					}
				}
			}
		}

		internal static async Task SendAlert(string message, string contentType)
		{
			try
			{
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				
				StringContent content = new StringContent(message);
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
			StringBuilder Xml = new StringBuilder();

			using (XmlDatabaseExport Output = new XmlDatabaseExport(Xml, true, 256))
			{
				await Database.Export(Output);
			}

			string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileName = Path.Combine(AppDataFolder, FileName);

			string CurrentState = Xml.ToString();
			string PrevState = File.Exists(FileName) ? File.ReadAllText(FileName) : string.Empty;

			EditScript<string> Script = Difference.AnalyzeRows(PrevState, CurrentState);
			StringBuilder Markdown = new StringBuilder();
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
	}
}
