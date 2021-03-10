using IdApp.Services;
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
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Content.Markdown;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.P2P;
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
        private Timer autoSaveTimer;
        private readonly ITagIdSdk sdk;
        private readonly IImageCacheService imageCacheService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly IThingRegistryOrchestratorService thingRegistryOrchestratorService;
        private readonly bool keepRunningInTheBackground = false;
        private Profiler startupProfiler;

        ///<inheritdoc/>
        public App()
        {
            this.startupProfiler = new Profiler("Startup", ProfilerThreadType.Sequential);  // Comment out to remove startup profiling.
            this.startupProfiler?.Start();
            this.startupProfiler?.NewState("Init");

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            InitializeComponent();

            try
            {
                this.startupProfiler?.NewState("Types");

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
                        typeof(XML).Assembly,                       // XML Content-Type
                        typeof(MarkdownDocument).Assembly,          // Markdown support
                        typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
                        typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
                        typeof(ProvisioningClient).Assembly,        // Serialization of XMPP objects related to thing registries, provisioning and decision support.
                        typeof(SensorClient).Assembly,              // Serialization of XMPP objects related to sensors
                        typeof(ControlClient).Assembly,             // Serialization of XMPP objects related to actuators
                        typeof(ConcentratorClient).Assembly,        // Serialization of XMPP objects related to concentrators
                        typeof(Expression).Assembly,                // Indexes basic script functions
                        typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
                        typeof(TagConfiguration).Assembly,          // Indexes persistable objects
                        typeof(RegistrationStep).Assembly);         // Indexes persistable objects
                }

                this.startupProfiler?.NewState("SDK");
                // Create Services
                this.sdk = TagIdSdk.Create(appAssembly, this.startupProfiler, new XmppConfiguration().ToArray());
                this.imageCacheService = new ImageCacheService(this.sdk.SettingsService, this.sdk.LogService);
                this.sdk.RegisterSingleton<IImageCacheService, ImageCacheService>(this.imageCacheService);
                this.contractOrchestratorService = new ContractOrchestratorService(this.sdk.TagProfile, this.sdk.UiDispatcher, this.sdk.NeuronService, this.sdk.NavigationService, this.sdk.LogService, this.sdk.NetworkService);
                this.sdk.RegisterSingleton<IContractOrchestratorService, ContractOrchestratorService>(this.contractOrchestratorService);
                this.thingRegistryOrchestratorService = new ThingRegistryOrchestratorService(this.sdk.TagProfile, this.sdk.UiDispatcher, this.sdk.NeuronService, this.sdk.NavigationService, this.sdk.LogService, this.sdk.NetworkService);
                this.sdk.RegisterSingleton<IThingRegistryOrchestratorService, ThingRegistryOrchestratorService>(this.thingRegistryOrchestratorService);

                // Set resolver
                DependencyResolver.ResolveUsing(type =>
                {
                    object obj = this.sdk.Resolve(type);
                    if (!(obj is null))
                        return obj;

                    if (Types.GetType(type.FullName) is null)
                        return null;    // Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.

                    return Types.Instantiate(true, type);
                });

                // Get the db started right away to save startup time.
                this.sdk.StorageService.Init(this.startupProfiler?.CreateThread("Database", ProfilerThreadType.Sequential));

                // Register log listener (optional)
                this.sdk.LogService.AddListener(new AppCenterEventSink(this.sdk.LogService));
            }
            catch (Exception e)
            {
                e = Waher.Events.Log.UnnestException(e);
                this.startupProfiler?.Exception(e);
                DisplayBootstrapErrorPage(e.Message, e.StackTrace);
                return;
            }

            // Start page
            try
            {
                this.startupProfiler?.NewState("MainPage");

                this.MainPage = new AppShell();
            }
            catch (Exception e)
            {
                e = Waher.Events.Log.UnnestException(e);
                this.startupProfiler?.Exception(e);
                this.sdk.LogService.SaveExceptionDump("StartPage", e.ToString());
            }

            this.startupProfiler?.MainThread.Idle();
        }

        #region Startup/Shutdown

        ///<inheritdoc/>
        protected override async void OnStart()
        {
            //AppCenter.Start(
            //    "android={Your Android App secret here};uwp={Your UWP App secret here};ios={Your iOS App secret here}",
            //    typeof(Analytics),
            //    typeof(Crashes));

            await this.PerformStartup(false);
        }

        ///<inheritdoc/>
        protected override async void OnResume()
        {
            await this.PerformStartup(true);
        }

        private async Task PerformStartup(bool isResuming)
        {
            ProfilerThread thread = this.startupProfiler?.MainThread.CreateSubThread("AppStartup", ProfilerThreadType.Sequential);
            thread?.Start();

            try
            {
                thread?.NewState("Report");
                await this.SendErrorReportFromPreviousRun();

                thread?.NewState("Startup");
                ProfilerThread sdkStartupThread = this.startupProfiler?.CreateThread("SdkStartup", ProfilerThreadType.Sequential);
                sdkStartupThread?.Start();
                sdkStartupThread?.NewState("DB");

                this.sdk.UiDispatcher.IsRunningInTheBackground = false;

                // Start the db.
                // This is for soft restarts.
                // If this is a cold start, this call is made already in the App ctor, and this is then a no-op.
                this.sdk.StorageService.Init(sdkStartupThread);
                StorageState dbState = await this.sdk.StorageService.WaitForReadyState();
                if (dbState == StorageState.NeedsRepair)
                {
                    await this.sdk.StorageService.TryRepairDatabase(sdkStartupThread);
                }

                if (!isResuming)
                {
                    await this.CreateOrRestoreConfiguration();
                }

                sdkStartupThread?.NewState("Network");
                await this.sdk.NetworkService.Load(isResuming);

                sdkStartupThread?.NewState("Load");
                await this.sdk.NeuronService.Load(isResuming);

                sdkStartupThread?.NewState("Timer");
                TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
                this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

                sdkStartupThread?.Stop();

                thread?.NewState("Cache");
                await this.imageCacheService.Load(isResuming);

                thread?.NewState("Orchestrators");
                await this.contractOrchestratorService.Load(isResuming);
                await this.thingRegistryOrchestratorService.Load(isResuming);
            }
            catch (Exception e)
            {
                e = Waher.Events.Log.UnnestException(e);
                thread?.Exception(e);
                this.DisplayBootstrapErrorPage(e.Message, e.StackTrace);
            }

            thread?.Stop();
            this.StartupCompleted("StartupProfile.uml", false);
        }

        ///<inheritdoc/>
        protected override async void OnSleep()
        {
            // Done manually here, as the Disappearing event won't trigger when exiting the app,
            // and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.
            if (MainPage?.BindingContext is BaseViewModel vm)
            {
                await vm.Shutdown();
            }

            await this.Shutdown(false);
        }

        private async Task Shutdown(bool inPanic)
        {
            StopAutoSaveTimer();
            if (!(this.sdk.UiDispatcher is null))
            {
                this.sdk.UiDispatcher.IsRunningInTheBackground = !inPanic;
            }

            if (inPanic)
            {
                if (!(this.sdk.NeuronService is null))
                    await this.sdk.NeuronService.UnloadFast();
            }
            else if (!this.keepRunningInTheBackground)
            {
                if (!(this.contractOrchestratorService is null))
                    await this.contractOrchestratorService.Unload();

                if (!(this.thingRegistryOrchestratorService is null))
                    await this.thingRegistryOrchestratorService.Unload();

                if (!(this.sdk.NeuronService is null))
                    await this.sdk.NeuronService.Unload();
                
                if (!(this.sdk.NetworkService is null))
                    await this.sdk.NetworkService.Unload();
                
                if (!(this.imageCacheService is null))
                    await this.imageCacheService.Unload();
            }

            await Types.StopAllModules();
            Waher.Events.Log.Terminate();
            if (!(this.sdk.StorageService is null))
            {
                await this.sdk.StorageService.Shutdown();
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
            if (this.sdk.TagProfile.IsDirty)
            {
                this.sdk.TagProfile.ResetIsDirty();
                try
                {
                    TagConfiguration tc = this.sdk.TagProfile.ToConfiguration();
                    try
                    {
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
            if (!(e.Exception?.InnerException is null)) // Unwrap the AggregateException
            {
                ex = e.Exception.InnerException;
            }
            e.SetObserved();
            await Handle_UnhandledException(ex, nameof(TaskScheduler_UnobservedTaskException), false);
        }

        private async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await Handle_UnhandledException(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), true);
        }

        private async Task Handle_UnhandledException(Exception ex, string title, bool shutdown)
        {
            if (!(ex is null))
            {
                this.sdk.LogService.SaveExceptionDump(title, ex.ToString());
            }

            if (!(ex is null))
            {
                this.sdk.LogService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
            }

            if (shutdown)
            {
                await this.Shutdown(true);
            }

#if DEBUG
            if (!shutdown)
            {
                if (Device.IsInvokeRequired && !(MainPage is null))
                {
                    Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok));
                }
                else if (!(MainPage is null))
                {
                    await MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok);
                }
            }
#endif
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            this.startupProfiler?.Exception(e.Exception);
        }

        private void DisplayBootstrapErrorPage(string title, string stackTrace)
        {
            this.sdk.LogService?.SaveExceptionDump(title, stackTrace);

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
        }

        private async Task SendErrorReportFromPreviousRun()
        {
            if (!(this.sdk.LogService is null))
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

                string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                ProfileFileName = Path.Combine(AppDataFolder, ProfileFileName);

                string uml = this.startupProfiler.ExportPlantUml(TimeUnit.MilliSeconds);
                File.WriteAllText(ProfileFileName, uml);

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
            Dictionary<string, bool> CollectionNames = new Dictionary<string, bool>();

            foreach (string CollectionName in await Database.GetCollections())
                CollectionNames[CollectionName] = true;

            CollectionNames.Remove("DnsCache");

            string[] Selection = new string[CollectionNames.Count];
            CollectionNames.Keys.CopyTo(Selection, 0);

            StringBuilder Xml = new StringBuilder();

            using (XmlDatabaseExport Output = new XmlDatabaseExport(Xml, true, 256))
            {
                await Database.Export(Output, Selection);
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
