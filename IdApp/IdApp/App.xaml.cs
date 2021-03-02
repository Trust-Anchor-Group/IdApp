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
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
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
        private readonly ITagProfile tagProfile;
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly ICryptoService cryptoService;
        private readonly INetworkService networkService;
        private readonly ISettingsService settingsService;
        private readonly IStorageService storageService;
        private readonly INavigationService navigationService;
        private readonly INeuronService neuronService;
        private readonly IImageCacheService imageCacheService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly bool keepRunningInTheBackground = false;
        private Profiler startupProfiler;
        private readonly DomainModel[] domainModels;

        ///<inheritdoc/>
        public App()
        {
            this.startupProfiler = new Profiler("Startup", ProfilerThreadType.Sequential);  // Comment out to remove startup profiling.
            this.startupProfiler?.Start();
            this.startupProfiler?.NewState("Init");

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            this.domainModels = new XmppConfiguration().ToArray();

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
                        typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
                        typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
                        typeof(Expression).Assembly,                // Indexes basic script functions
                        typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
                        typeof(TagConfiguration).Assembly,          // Indexes persistable objects
                        typeof(RegistrationStep).Assembly);         // Indexes persistable objects
                }

                this.startupProfiler?.NewState("SDK");
                // Create Services
                Types.SetModuleParameter("AppAssembly", appAssembly);
                this.tagProfile = Types.InstantiateDefault<ITagProfile>(false, (object)this.domainModels);
                this.logService = Types.Instantiate<ILogService>(false);
                this.uiDispatcher = Types.Instantiate<IUiDispatcher>(false);
                this.cryptoService = Types.Instantiate<ICryptoService>(false);
                this.networkService = Types.Instantiate<INetworkService>(false);
                this.settingsService = Types.Instantiate<ISettingsService>(false);
                this.storageService = Types.Instantiate<IStorageService>(false);
                this.navigationService = Types.Instantiate<INavigationService>(false);
                this.neuronService = Types.Instantiate<INeuronService>(false);
                this.imageCacheService = Types.Instantiate<IImageCacheService>(false);
                this.contractOrchestratorService = Types.Instantiate<IContractOrchestratorService>(false);

                // Set resolver
                DependencyResolver.ResolveUsing(type =>
                {
                    if (Types.GetType(type.FullName) is null)
                        return null;    // Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.

                    return Types.Instantiate(true, type);
                });

                // Get the db started right away to save startup time.
                this.storageService.Init(this.startupProfiler?.CreateThread("Database", ProfilerThreadType.Sequential));

                // Register log listener (optional)
                this.logService.AddListener(new AppCenterEventSink(this.logService));
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
                this.logService.SaveExceptionDump("StartPage", e.ToString());
            }

            this.startupProfiler?.MainThread.Idle();
        }

        #region Startup/Shutdown

        private void ReRegisterServices()
        {
            if (!Types.IsSingletonRegistered(typeof(ITagProfile), (object)this.domainModels))
                Types.RegisterSingleton(this.tagProfile, (object)this.domainModels);

            if (!Types.IsSingletonRegistered(typeof(ILogService)))
                Types.RegisterSingleton(this.logService);

            if (!Types.IsSingletonRegistered(typeof(IUiDispatcher)))
                Types.RegisterSingleton(this.uiDispatcher);

            if (!Types.IsSingletonRegistered(typeof(ICryptoService)))
                Types.RegisterSingleton(this.cryptoService);

            if (!Types.IsSingletonRegistered(typeof(INetworkService)))
                Types.RegisterSingleton(this.networkService);

            if (!Types.IsSingletonRegistered(typeof(ISettingsService)))
                Types.RegisterSingleton(this.settingsService);

            if (!Types.IsSingletonRegistered(typeof(IStorageService)))
                Types.RegisterSingleton(this.storageService);

            if (!Types.IsSingletonRegistered(typeof(INavigationService)))
                Types.RegisterSingleton(this.navigationService);

            if (!Types.IsSingletonRegistered(typeof(INeuronService)))
                Types.RegisterSingleton(this.neuronService);

            if (!Types.IsSingletonRegistered(typeof(IImageCacheService)))
                Types.RegisterSingleton(this.imageCacheService);

            if (!Types.IsSingletonRegistered(typeof(IContractOrchestratorService)))
                Types.RegisterSingleton(this.contractOrchestratorService);
        }

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
                if(isResuming)
                {
                    this.ReRegisterServices();
                }

                thread?.NewState("Report");
                await this.SendErrorReportFromPreviousRun();

                thread?.NewState("Startup");
                ProfilerThread sdkStartupThread = this.startupProfiler?.CreateThread("SdkStartup", ProfilerThreadType.Sequential);
                sdkStartupThread?.Start();
                sdkStartupThread?.NewState("DB");

                this.uiDispatcher.IsRunningInTheBackground = false;

                // Start the db.
                // This is for soft restarts.
                // If this is a cold start, this call is made already in the App ctor, and this is then a no-op.
                this.storageService.Init(sdkStartupThread);
                StorageState dbState = await this.storageService.WaitForReadyState();
                if (dbState == StorageState.NeedsRepair)
                {
                    await this.storageService.TryRepairDatabase(sdkStartupThread);
                }

                if (!isResuming)
                {
                    await this.CreateOrRestoreConfiguration();
                }

                sdkStartupThread?.NewState("Network");

                await this.networkService.Load(isResuming);

                sdkStartupThread?.NewState("Load");

                await this.neuronService.Load(isResuming);

                sdkStartupThread?.NewState("Timer");

                TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
                this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

                sdkStartupThread?.Stop();

                thread?.NewState("Cache");
                await this.imageCacheService.Load(isResuming);

                thread?.NewState("Orchestrator");
                await this.contractOrchestratorService.Load(isResuming);
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
            if (this.uiDispatcher != null)
            {
                this.uiDispatcher.IsRunningInTheBackground = !inPanic;
            }

            if (inPanic)
            {
                if (this.neuronService != null)
                    await this.neuronService.UnloadFast();
            }
            else if (!this.keepRunningInTheBackground)
            {
                if (this.contractOrchestratorService != null)
                    await this.contractOrchestratorService.Unload();
                if (this.neuronService != null)
                    await this.neuronService.Unload();
                if (this.networkService != null)
                    await this.networkService.Unload();
                if (this.imageCacheService != null)
                    await this.imageCacheService.Unload();
            }

            await Types.StopAllModules();
            Waher.Events.Log.Terminate();
            if (this.storageService != null)
            {
                await this.storageService.Shutdown();
            }
        }

        #endregion

        private void StopAutoSaveTimer()
        {
            if (this.autoSaveTimer != null)
            {
                this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.autoSaveTimer.Dispose();
                this.autoSaveTimer = null;
            }
        }

        private async Task AutoSave()
        {
            if (this.tagProfile.IsDirty)
            {
                this.tagProfile.ResetIsDirty();
                try
                {
                    TagConfiguration tc = this.tagProfile.ToConfiguration();
                    try
                    {
                        await this.storageService.Update(tc);
                    }
                    catch (KeyNotFoundException)
                    {
                        await this.storageService.Insert(tc);
                    }
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                }
            }
        }

        private async Task CreateOrRestoreConfiguration()
        {
            TagConfiguration configuration;

            try
            {
                configuration = await this.storageService.FindFirstDeleteRest<TagConfiguration>();
            }
            catch (Exception findException)
            {
                this.logService.LogException(findException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                configuration = null;
            }

            if (configuration == null)
            {
                configuration = new TagConfiguration();
                try
                {
                    await this.storageService.Insert(configuration);
                }
                catch (Exception insertException)
                {
                    this.logService.LogException(insertException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                }
            }

            this.tagProfile.FromConfiguration(configuration);
        }

        #region Error Handling

        private async void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            if (e.Exception?.InnerException != null) // Unwrap the AggregateException
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
            if (ex != null)
            {
                this.logService.SaveExceptionDump(title, ex.ToString());
            }

            if (ex != null)
            {
                this.logService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
            }

            if (shutdown)
            {
                await this.Shutdown(true);
            }

#if DEBUG
            if (!shutdown)
            {
                if (Device.IsInvokeRequired && MainPage != null)
                {
                    Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert(title, ex?.ToString(), AppResources.Ok));
                }
                else if (MainPage != null)
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
            this.logService?.SaveExceptionDump(title, stackTrace);

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
            if (this.logService != null)
            {
                string stackTrace = this.logService.LoadExceptionDump();
                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    try
                    {
                        await SendAlert(stackTrace, "text/plain");
                    }
                    finally
                    {
                        this.logService.DeleteExceptionDump();
                    }
                }
            }
        }

        public static async Task SendAlert(string message, string contentType)
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
