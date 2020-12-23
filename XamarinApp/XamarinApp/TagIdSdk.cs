using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
using XamarinApp.Services;

namespace XamarinApp
{
    public class TagIdSdk : ITagIdSdk
    {
        private static ITagIdSdk instance;

        private FilesProvider filesProvider;

        private TagIdSdk()
        {
            this.TagProfile = new TagProfile();
            this.logService = new LogService(DependencyService.Resolve<IAppInformation>());
            this.AuthService = new AuthService(this.LogService);
            this.NetworkService = new NetworkService(this.LogService);
            this.SettingsService = new SettingsService();
            this.StorageService = new StorageService();
            this.neuronService = new NeuronService(this.TagProfile, this.NetworkService, this.logService);
            this.NavigationService = new NavigationService();
            this.ContractsService = new ContractsService(this.TagProfile, this.NeuronService, this.NavigationService, this.LogService);
        }

        public void Dispose()
        {
            instance = null;
        }

        public static ITagIdSdk Create()
        {
            return instance ?? (instance = new TagIdSdk());
        }

        public TagProfile TagProfile { get; }
        public IAuthService AuthService { get; }
        private readonly IInternalNeuronService neuronService;
        public INeuronService NeuronService => neuronService;
        public IContractsService ContractsService { get; }
        public INetworkService NetworkService { get; }
        public INavigationService NavigationService { get; }
        public IStorageService StorageService { get; }
        public ISettingsService SettingsService { get; }
        private readonly IInternalLogService logService;
        public ILogService LogService => logService;

        public async Task Startup()
        {
            Types.Initialize(
                typeof(App).Assembly,
                typeof(Database).Assembly,
                typeof(FilesProvider).Assembly,
                typeof(ObjectSerializer).Assembly,
                typeof(XmppClient).Assembly,
                typeof(ContractsClient).Assembly,
                typeof(Expression).Assembly,
                typeof(XmppServerlessMessaging).Assembly);

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");
            if (filesProvider == null)
            {
                filesProvider = await FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.AuthService.GetCustomKey);
            }
            await filesProvider.RepairIfInproperShutdown(string.Empty);
            Database.Register(filesProvider, false);

            TagConfiguration configuration = await this.StorageService.FindFirstDeleteRest<TagConfiguration>();
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                await this.StorageService.Insert(configuration);
            }
            this.TagProfile.FromConfiguration(configuration);

            await this.NeuronService.Load();
        }

        public Task Shutdown()
        {
            return Shutdown(false);
        }

        public Task ShutdownInPanic()
        {
            return Shutdown(true);
        }

        private async Task Shutdown(bool panic)
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