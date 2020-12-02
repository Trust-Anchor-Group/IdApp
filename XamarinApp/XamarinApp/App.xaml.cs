using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Script;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.ViewModels;

namespace XamarinApp
{
	public partial class App : IDisposable
    {
        private const string TagConfigurationSettingKey = "TagConfiguration";
        private Timer autoSaveTimer;
        private readonly INeuronService neuronService;
        private readonly ISettingsService settingsService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly TagProfile tagProfile;
        private FilesProvider filesProvider;

        public App()
		{
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			InitializeComponent();
			this.tagProfile = new TagProfile();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(this.tagProfile).SingleInstance();
			builder.RegisterType<NeuronService>().As<INeuronService>().SingleInstance();
			builder.RegisterType<ContractsService>().As<IContractsService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			//builder.RegisterType<StorageService>().As<IStorageService>().SingleInstance();
			builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
			builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
			builder.RegisterType<ContractOrchestratorService>().As<IContractOrchestratorService>().SingleInstance();
            IContainer container = builder.Build();
			// Set AutoFac to be the dependency resolver
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			// Resolve what's needed for the App class
			this.neuronService = DependencyService.Resolve<INeuronService>();
            this.settingsService = DependencyService.Resolve<ISettingsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();

            Types.Initialize(
                typeof(App).Assembly,
                typeof(Database).Assembly,
                typeof(FilesProvider).Assembly,
                typeof(ObjectSerializer).Assembly,
                typeof(XmppClient).Assembly,
                typeof(ContractsClient).Assembly,
                typeof(Expression).Assembly,
                //typeof(Waher.Things.ThingReference).Assembly,
                //typeof(RuntimeSettings).Assembly,
                typeof(XmppServerlessMessaging).Assembly);
            //typeof(ProvisioningClient).Assembly);

            // Start page
            NavigationPage navigationPage = new NavigationPage(new InitPage())
            {
                BarBackgroundColor = (Color) Application.Current.Resources["HeadingBackground"],
                BarTextColor = (Color) Application.Current.Resources["HeadingForeground"]
            };
            this.MainPage = navigationPage;
        }

        private async void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            if (e.Exception?.InnerException != null)
            {
                ex = e.Exception.InnerException;
            }
            e.SetObserved();

            if (Device.IsInvokeRequired)
            {
                Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert("Unhandled Task Exception", ex?.ToString(), AppResources.Ok));
            }
            else
            {
                await MainPage.DisplayAlert("Unhandled Task Exception", ex?.ToString(), AppResources.Ok);
            }
        }

        private async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (Device.IsInvokeRequired)
            {
                Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert("Unhandled Exception", ex?.ToString(), AppResources.Ok));
            }
            else
            {
                await MainPage.DisplayAlert("Unhandled Exception", ex?.ToString(), AppResources.Ok);
            }
        }

        public void Dispose()
        {
            //DatabaseModule.Flush().GetAwaiter().GetResult();
            Types.StopAllModules().GetAwaiter().GetResult();
        }

        protected override async void OnStart()
        {
            await PerformStartup();
        }

		protected override async void OnResume()
        {
            await PerformStartup();
        }

        protected override async void OnSleep()
        {
            await PerformShutdown();
        }

		private async Task PerformStartup()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");
            if (filesProvider == null)
            {
                IAuthService authService = DependencyService.Resolve<IAuthService>();
                filesProvider = await FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, 10000, authService.GetCustomKey);
            }
            await filesProvider.RepairIfInproperShutdown(string.Empty);
            Database.Register(filesProvider, false);

            await this.neuronService.Load();
            await this.contractOrchestratorService.Load();

            TagConfiguration configuration = this.settingsService.RestoreState<TagConfiguration>(TagConfigurationSettingKey);
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                this.settingsService.SaveState(TagConfigurationSettingKey, configuration);
            }
            this.tagProfile.FromConfiguration(configuration);

            this.autoSaveTimer = new Timer(_ => AutoSave(), null, Constants.Intervals.AutoSave, Constants.Intervals.AutoSave);
        }

        private async Task PerformShutdown()
        {
            await DatabaseModule.Flush();
            this.filesProvider.Dispose();
            this.filesProvider = null;

            this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.autoSaveTimer.Dispose();
            AutoSave();
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            await this.contractOrchestratorService.Unload();
            await this.neuronService.Unload();
        }

        private void AutoSave()
        {
            if (this.tagProfile.IsDirty)
            {
                this.tagProfile.ResetIsDirty();
                this.settingsService.SaveState(TagConfigurationSettingKey, this.tagProfile.ToConfiguration());
            }
        }
    }
}
