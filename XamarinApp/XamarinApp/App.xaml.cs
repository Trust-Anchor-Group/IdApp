﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
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
        private readonly ITagIdSdk sdk;
        private readonly ISettingsService settingsService;
        private readonly IContractOrchestratorService contractOrchestratorService;

        public App()
		{
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			InitializeComponent();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
            this.sdk = TagIdSdk.Create();
            builder.RegisterInstance(this.sdk.TagProfile).SingleInstance();
			builder.RegisterInstance(this.sdk.NeuronService).SingleInstance();
			builder.RegisterInstance(this.sdk.AuthService).SingleInstance();
			builder.RegisterInstance(this.sdk.NetworkService).SingleInstance();

			builder.RegisterType<ContractsService>().As<IContractsService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
			builder.RegisterType<ContractOrchestratorService>().As<IContractOrchestratorService>().SingleInstance();
            IContainer container = builder.Build();
			// Set AutoFac to be the dependency resolver
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			// Resolve what's needed for the App class
            this.settingsService = DependencyService.Resolve<ISettingsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();

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
            this.sdk.Dispose();
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
            await this.sdk.Startup();
            
            await this.contractOrchestratorService.Load();

            TagConfiguration configuration = this.settingsService.RestoreState<TagConfiguration>(TagConfigurationSettingKey);
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                this.settingsService.SaveState(TagConfigurationSettingKey, configuration);
            }
            this.sdk.TagProfile.FromConfiguration(configuration);

            this.autoSaveTimer = new Timer(_ => AutoSave(), null, Constants.Intervals.AutoSave, Constants.Intervals.AutoSave);
        }

        private async Task PerformShutdown()
        {
            this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.autoSaveTimer.Dispose();
            AutoSave();
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            await this.contractOrchestratorService.Unload();
            await this.sdk.Shutdown();
        }

        private void AutoSave()
        {
            if (this.sdk.TagProfile.IsDirty)
            {
                this.sdk.TagProfile.ResetIsDirty();
                this.settingsService.SaveState(TagConfigurationSettingKey, this.sdk.TagProfile.ToConfiguration());
            }
        }
    }
}
