using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using Device = Xamarin.Forms.Device;

namespace XamarinApp
{
	public partial class App : IDisposable
    {
        private Timer autoSaveTimer;
        private readonly ITagIdSdk sdk;
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
			builder.RegisterInstance(this.sdk.ContractsService).SingleInstance();
			builder.RegisterInstance(this.sdk.AuthService).SingleInstance();
			builder.RegisterInstance(this.sdk.NetworkService).SingleInstance();
			builder.RegisterInstance(this.sdk.LogService).SingleInstance();
			builder.RegisterInstance(this.sdk.StorageService).SingleInstance();
			builder.RegisterInstance(this.sdk.SettingsService).SingleInstance();

			builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
			builder.RegisterType<ContractOrchestratorService>().As<IContractOrchestratorService>().SingleInstance();
            IContainer container = builder.Build();
			// Set AutoFac to be the dependency resolver
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			// Resolve what's needed for the App class
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
            this.sdk.LogService.LogException(ex, new KeyValuePair<string, string>("TaskScheduler", "UnobservedTaskException"));
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
            if (ex != null)
            {
                this.sdk.LogService.LogException(ex, new KeyValuePair<string, string>("CurrentDomain", "UnhandledException"));
            }
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
            AppCenter.Start(
                "android=972ae016-29c4-4e4f-af9a-ad7eebfca1f7;uwp={Your UWP App secret here};ios={Your iOS App secret here}",
                typeof(Analytics),
                typeof(Crashes)); 
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
            this.sdk.AutoSave();
        }
    }
}
