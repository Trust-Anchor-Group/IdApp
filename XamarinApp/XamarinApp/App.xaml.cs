using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Waher.Events;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Script;
using Waher.Script.Graphs;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using Log = Waher.Events.Log;

namespace XamarinApp
{
	public partial class App : IDisposable
	{
        private Timer autoSaveTimer;
        private InternalSink internalSink;
        private readonly INeuronService neuronService;
        private readonly IStorageService storageService;
        private readonly IIdentityOrchestratorService identityOrchestratorService;
        private readonly TagProfile tagProfile;

		public App()
		{
			InitializeComponent();
			this.tagProfile = new TagProfile();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(this.tagProfile).SingleInstance();
			builder.RegisterType<NeuronService>().As<INeuronService>().SingleInstance();
			builder.RegisterType<ContractsService>().As<IContractsService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			builder.RegisterType<StorageService>().As<IStorageService>().SingleInstance();
			builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
			builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
			builder.RegisterType<IdentityOrchestratorService>().As<IIdentityOrchestratorService>().SingleInstance();
            IContainer container = builder.Build();
			// Set AutoFac to be the dependency resolver
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			// Resolve what's needed for the App class
			this.neuronService = DependencyService.Resolve<INeuronService>();
            this.storageService = DependencyService.Resolve<IStorageService>();
            this.identityOrchestratorService = DependencyService.Resolve<IIdentityOrchestratorService>();
            this.internalSink = new InternalSink();
            Log.Register(this.internalSink);

            Types.Initialize(
                typeof(App).Assembly,
                typeof(Database).Assembly,
                typeof(FilesProvider).Assembly,
                typeof(ObjectSerializer).Assembly,
                typeof(XmppClient).Assembly,
                typeof(ContractsClient).Assembly,
                typeof(Expression).Assembly,
                typeof(Graph).Assembly,
                typeof(Waher.Things.ThingReference).Assembly,
                typeof(RuntimeSettings).Assembly,
                typeof(Waher.Runtime.Language.Language).Assembly,
                typeof(DnsResolver).Assembly,
                typeof(Waher.Networking.XMPP.Sensor.SensorClient).Assembly,
                typeof(Waher.Networking.XMPP.Control.ControlClient).Assembly,
                typeof(Waher.Networking.XMPP.Concentrator.ConcentratorClient).Assembly,
                typeof(XmppServerlessMessaging).Assembly,
                typeof(ProvisioningClient).Assembly,
                typeof(Waher.Security.EllipticCurves.EllipticCurve).Assembly);

			// Start page
            this.MainPage = new NavigationPage(new InitPage());
        }

        public void Dispose()
        {
            DatabaseModule.Flush().GetAwaiter().GetResult();
            Types.StopAllModules().GetAwaiter().GetResult();
            Log.Unregister(this.internalSink);
            Log.Terminate();
            this.internalSink.Dispose();
            this.internalSink = null;
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
            await this.storageService.Load();
            await this.neuronService.Load();
            await this.identityOrchestratorService.Load();

            TagConfiguration configuration = await this.storageService.FindFirstDeleteRest<TagConfiguration>();
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                await this.storageService.Insert(configuration);
            }
            this.tagProfile.FromConfiguration(configuration);

            this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, Constants.Intervals.AutoSave, Constants.Intervals.AutoSave);
        }

        private async Task PerformShutdown()
        {
            this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.autoSaveTimer.Dispose();
            await AutoSave();
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            await this.identityOrchestratorService.Unload();
            await this.neuronService.Unload();
            await this.storageService.Unload();
        }

        private async Task AutoSave()
        {
            if (this.tagProfile.IsDirty)
            {
                this.tagProfile.ResetIsDirty();
                await this.storageService.Update(this.tagProfile.ToConfiguration());
            }
        }

        private class InternalSink : EventSink
        {
            public InternalSink()
                : base("InternalEventSink")
            {
            }

            public override Task Queue(Event _)
            {
                return Task.CompletedTask;
            }
        }
    }
}
