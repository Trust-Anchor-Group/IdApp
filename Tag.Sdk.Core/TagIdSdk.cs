using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tag.Sdk.Core.Models;
using Tag.Sdk.Core.Services;
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

namespace Tag.Sdk.Core
{
    public class TagIdSdk : ITagIdSdk
    {
        private static ITagIdSdk instance;
        private readonly Assembly appAssembly;
        private FilesProvider filesProvider;

        private TagIdSdk(Application app, params DomainModel[] domains)
        {
            this.TagProfile = new TagProfile(domains);
            this.logService = new LogService(DependencyService.Resolve<IAppInformation>());
            this.uiDispatcher = new UiDispatcher();
            this.AuthService = new AuthService(this.LogService);
            this.NetworkService = new NetworkService(this.LogService, this.UiDispatcher);
            this.SettingsService = new SettingsService();
            this.StorageService = new StorageService();
            this.appAssembly = app.GetType().Assembly;
            this.neuronService = new NeuronService(this.appAssembly, this.TagProfile, this.UiDispatcher, this.NetworkService, this.logService);
            this.NavigationService = new NavigationService(DependencyService.Resolve<IUiDispatcher>());
        }

        public void Dispose()
        {
            instance = null;
        }

        public static ITagIdSdk Create(Application app, params DomainModel[] domains)
        {
            return instance ?? (instance = new TagIdSdk(app, domains));
        }

        public TagProfile TagProfile { get; }
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
                Types.Initialize(
                    this.appAssembly,
                    typeof(Database).Assembly,
                    typeof(FilesProvider).Assembly,
                    typeof(ObjectSerializer).Assembly,
                    typeof(XmppClient).Assembly,
                    typeof(ContractsClient).Assembly,
                    typeof(Expression).Assembly,
                    typeof(XmppServerlessMessaging).Assembly);
            }

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");
            if (filesProvider == null)
            {
                filesProvider = await FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.AuthService.GetCustomKey);
            }
            await filesProvider.RepairIfInproperShutdown(string.Empty);
            Database.Register(filesProvider, false);

            if (!isResuming)
            {
                TagConfiguration configuration = await this.StorageService.FindFirstDeleteRest<TagConfiguration>();
                if (configuration == null)
                {
                    configuration = new TagConfiguration();
                    await this.StorageService.Insert(configuration);
                }
                this.TagProfile.FromConfiguration(configuration);
            }

            await this.NeuronService.Load(isResuming);
        }

        public Task Shutdown(bool keepRunningInTheBackground)
        {
            this.uiDispatcher.IsRunningInTheBackground = true;
            if (keepRunningInTheBackground)
            {
                return Task.CompletedTask;
            }
            return Shutdown(false);
        }

        public Task ShutdownInPanic()
        {
            return ShutdownInternal(true);
        }

        private async Task ShutdownInternal(bool panic)
        {
            if (panic)
                await this.neuronService.UnloadFast();
            else
                await this.neuronService.Unload();
            await Types.StopAllModules();
            if (this.filesProvider != null)
            {
                this.filesProvider.Dispose();
                this.filesProvider = null;
            }
            Log.Terminate();
        }

        public void AutoSave()
        {
            if (this.TagProfile.IsDirty)
            {
                this.TagProfile.ResetIsDirty();
                this.StorageService.Update(this.TagProfile.ToConfiguration());
            }
        }
    }
}