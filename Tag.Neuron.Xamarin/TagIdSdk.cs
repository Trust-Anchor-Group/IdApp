using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.Services;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Script;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin
{
    public class TagIdSdk : ITagIdSdk
    {
        private static ITagIdSdk instance;
        private readonly Assembly appAssembly;
        private readonly Assembly[] additionalAssemblies;
        private FilesProvider databaseProvider;
        private Timer autoSaveTimer;

        private TagIdSdk(Assembly appAssembly, Assembly[] additionalAssemblies, params DomainModel[] domains)
        {
            this.TagProfile = new TagProfile(domains);
            this.logService = new LogService(DependencyService.Resolve<IAppInformation>());
            this.uiDispatcher = new UiDispatcher();
            this.AuthService = new AuthService(this.LogService);
            this.NetworkService = new NetworkService(this.LogService, this.UiDispatcher);
            this.SettingsService = new SettingsService();
            this.StorageService = new StorageService();
            this.appAssembly = appAssembly;
            this.additionalAssemblies = additionalAssemblies;
            this.neuronService = new NeuronService(this.appAssembly, this.TagProfile, this.UiDispatcher, this.NetworkService, this.logService);
            this.NavigationService = new NavigationService(this.logService, this.uiDispatcher);
        }

        public void Dispose()
        {
            instance = null;
        }

        public static ITagIdSdk Create(Assembly appAssembly, Assembly[] additionalAssemblies, params DomainModel[] domains)
        {
            if (appAssembly == null)
            {
                throw new ArgumentException("Value cannot be null", nameof(appAssembly));
            }
            return instance ?? (instance = new TagIdSdk(appAssembly, additionalAssemblies, domains));
        }

        public ITagProfile TagProfile { get; }
        private readonly UiDispatcher uiDispatcher;
        public IUiDispatcher UiDispatcher => this.uiDispatcher;
        public IAuthService AuthService { get; }
        private readonly IInternalNeuronService neuronService;
        public INeuronService NeuronService => this.neuronService;
        public INetworkService NetworkService { get; }
        public INavigationService NavigationService { get; }
        public IStorageService StorageService { get; }
        public ISettingsService SettingsService { get; }
        private readonly IInternalLogService logService;
        public ILogService LogService => logService;

        public async Task Startup(bool isResuming)
        {
            this.uiDispatcher.IsRunningInTheBackground = false;
            if (!isResuming)
            {
                List<Assembly> assemblies = new List<Assembly>
                {
                    this.appAssembly,
                    typeof(Database).Assembly,
                    typeof(FilesProvider).Assembly,
                    typeof(ObjectSerializer).Assembly,
                    typeof(XmppClient).Assembly,
                    typeof(ContractsClient).Assembly,
                    typeof(Expression).Assembly,
                    typeof(XmppServerlessMessaging).Assembly,
                    typeof(TagConfiguration).Assembly
                };

                if (this.additionalAssemblies != null && this.additionalAssemblies.Length > 0)
                {
                    foreach (Assembly assembly in this.additionalAssemblies)
                    {
                        if (!assemblies.Contains(assembly))
                        {
                            assemblies.Add(assembly);
                        }
                    }
                }

                Types.Initialize(assemblies.ToArray());
            }

            await InitializeDatabase();

            if (!isResuming)
            {
                await CreateOrRestoreConfiguration();
            }

            await this.NeuronService.Load(isResuming);

            TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
            this.autoSaveTimer = new Timer(_ => AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);
        }

        public async Task Shutdown(bool keepRunningInTheBackground)
        {
            StopAutoSaveTimer();
            this.uiDispatcher.IsRunningInTheBackground = true;
            if (!keepRunningInTheBackground)
            {
                await this.neuronService.Unload();
            }
            await Types.StopAllModules();
            Log.Terminate();
            await this.databaseProvider.Flush();
            this.databaseProvider.Dispose();
            this.databaseProvider = null;
        }

        public async Task ShutdownInPanic()
        {
            StopAutoSaveTimer();
            this.uiDispatcher.IsRunningInTheBackground = false;
            await this.neuronService.UnloadFast();
            await Types.StopAllModules();
            Log.Terminate();
            await this.databaseProvider.Flush();
            this.databaseProvider.Dispose();
            this.databaseProvider = null;
        }

        private void StopAutoSaveTimer()
        {
            if (this.autoSaveTimer != null)
            {
                this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.autoSaveTimer.Dispose();
                this.autoSaveTimer = null;
            }
        }

        private void AutoSave()
        {
            if (this.TagProfile.IsDirty)
            {
                this.TagProfile.ResetIsDirty();
                try
                {
                    this.StorageService.Update(this.TagProfile.ToConfiguration());
                }
                catch (Exception updateException)
                {
                    this.logService.LogException(updateException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                }
            }
        }

        private async Task InitializeDatabase()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");

            Task<FilesProvider> CreateDatabaseFile()
            {
                return FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.AuthService.GetCustomKey);
            }

            string method = null;
            try
            {
                // 1. Try create database
                method = nameof(CreateDatabaseFile);
                this.databaseProvider = await CreateDatabaseFile();
                method = nameof(FilesProvider.RepairIfInproperShutdown);
                await this.databaseProvider.RepairIfInproperShutdown(string.Empty);
            }
            catch (Exception e1)
            {
                // Create failed.
                this.logService.LogException(e1, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

                try
                {
                    // 2. Try repair database
                    if (this.databaseProvider == null && Database.HasProvider)
                    {
                        // This is an attempt that _can_ work.
                        // During a soft restart, there _may_ be a provider registered already. If so, grab it.
                        this.databaseProvider = Database.Provider as FilesProvider;
                    }

                    method = nameof(FilesProvider.RepairIfInproperShutdown);
                    // Could throw a NullReferenceException, which is _ok_, as it is caught below.
                    // Reasoning: If we can't create a provider, and the database doesn't have one assigned either, we're in serious trouble.
                    await this.databaseProvider.RepairIfInproperShutdown(string.Empty);
                }
                catch (Exception e2)
                {
                    // Repair failed
                    this.logService.LogException(e2, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

                    if (await this.UiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseCorruptInfoText, AppResources.RepairAndContinue, AppResources.ContinueAnyway))
                    {
                        try
                        {
                            // 3. Delete and create a new empty database
                            method = "Delete database file(s) and create new empty database";
                            Directory.Delete(dataFolder, true);
                            this.databaseProvider = await CreateDatabaseFile();
                            await this.databaseProvider.RepairIfInproperShutdown(string.Empty);
                        }
                        catch (Exception e3)
                        {
                            // Delete and create new failed. We're out of options.
                            this.logService.LogException(e3, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));
                            await this.UiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseRepairFailedInfoText, AppResources.Ok);
                        }
                    }
                }
            }

            try
            {
                method = $"{nameof(Database)}.{nameof(Database.Register)}";
                Database.Register(databaseProvider, false);
            }
            catch (Exception e)
            {
                this.logService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));
            }
        }

        private async Task CreateOrRestoreConfiguration()
        {
            TagConfiguration configuration;

            try
            {
                configuration = await this.StorageService.FindFirstDeleteRest<TagConfiguration>();
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
                    await this.StorageService.Insert(configuration);
                }
                catch (Exception insertException)
                {
                    this.logService.LogException(insertException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                }
            }

            this.TagProfile.FromConfiguration(configuration);
        }
    }
}