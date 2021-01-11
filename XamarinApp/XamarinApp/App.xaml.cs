using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Tag.Sdk.Core;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using Device = Xamarin.Forms.Device;

namespace XamarinApp
{
	public partial class App : IDisposable
    {
        private Timer autoSaveTimer;
        private readonly ITagIdSdk sdk;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly INavigationOrchestratorService navigationOrchestratorService;
        private readonly bool keepRunningInTheBackground = false;

        private string stacktraceDuringStartup;

        public App()
		{
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                DisplayErrorPage("InitializeComponent", e.ToString());
                return;
            }

            try
            {
                this.sdk = TagIdSdk.Create(this, new Registration().ToArray());
                // Registrations
                ContainerBuilder builder = new ContainerBuilder();
                builder.RegisterInstance(this.sdk.UiDispatcher).SingleInstance();
                builder.RegisterInstance(this.sdk.TagProfile).SingleInstance();
                builder.RegisterInstance(this.sdk.NeuronService).SingleInstance();
                builder.RegisterInstance(this.sdk.AuthService).SingleInstance();
                builder.RegisterInstance(this.sdk.NetworkService).SingleInstance();
                builder.RegisterInstance(this.sdk.LogService).SingleInstance();
                builder.RegisterInstance(this.sdk.StorageService).SingleInstance();
                builder.RegisterInstance(this.sdk.SettingsService).SingleInstance();
                builder.RegisterInstance(this.sdk.NavigationService).SingleInstance();
                builder.RegisterType<ContractOrchestratorService>().As<IContractOrchestratorService>().SingleInstance();
                builder.RegisterType<NavigationOrchestratorService>().As<INavigationOrchestratorService>().SingleInstance();
                IContainer container = builder.Build();
                // Set AutoFac to be the dependency resolver
                DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

                // Resolve what's needed for the App class
                this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
                this.navigationOrchestratorService = DependencyService.Resolve<INavigationOrchestratorService>();
            }
            catch (Exception e)
            {
                DisplayErrorPage("ContainerBuilder", e.ToString());
                return;
            }

            if (!string.IsNullOrWhiteSpace(stacktraceDuringStartup))
            {
                DisplayErrorPage("Unhandled or Task Exception", stacktraceDuringStartup);
                return;
            }

            // Start page
            try
            {
                NavigationPage navigationPage = new NavigationPage(new InitPage());
                navigationPage.BarBackgroundColor = (Color)Current.Resources["HeadingBackground"];
                navigationPage.BarTextColor = (Color)Current.Resources["HeadingForeground"];

                this.MainPage = navigationPage;
            }
            catch (Exception e)
            {
                DisplayErrorPage("StartPage", e.ToString());
                return;
            }
        }

        private void DisplayErrorPage(string title, string stackTrace)
        {
            if (stacktraceDuringStartup == null)
            {
                stacktraceDuringStartup = stackTrace;
            }
            else
            {
                stacktraceDuringStartup = $"{stackTrace}{Environment.NewLine}{stacktraceDuringStartup}";
            }

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
                Text = stacktraceDuringStartup,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            });
            this.MainPage = new ContentPage
            {
                Content = sl
            };
        }

        private async void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            if (e.Exception?.InnerException != null)
            {
                ex = e.Exception.InnerException;
            }
            if (this.sdk?.LogService != null)
            {
                this.sdk.LogService.LogException(ex, new KeyValuePair<string, string>("TaskScheduler", "UnobservedTaskException"));
            }
            e.SetObserved();

            if (Device.IsInvokeRequired && MainPage != null)
            {
                Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert("Unhandled Task Exception", ex?.ToString(), AppResources.Ok));
            }
            else if (MainPage != null)
            {
                await MainPage.DisplayAlert("Unhandled Task Exception", ex?.ToString(), AppResources.Ok);
            }

            stacktraceDuringStartup = ex?.ToString();
        }

        private async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null && this.sdk?.LogService != null)
            {
                this.sdk.LogService.LogException(ex, new KeyValuePair<string, string>("CurrentDomain", "UnhandledException"));
            }

            if (this.sdk != null)
            {
                await this.sdk.ShutdownInPanic();
            }

            if (Device.IsInvokeRequired && MainPage != null)
            {
                Device.BeginInvokeOnMainThread(async () => await MainPage.DisplayAlert("Unhandled Exception", ex?.ToString(), AppResources.Ok));
            }
            else if (MainPage != null)
            {
                await MainPage.DisplayAlert("Unhandled Exception", ex?.ToString(), AppResources.Ok);
            }

            stacktraceDuringStartup = ex?.ToString();
        }

        public void Dispose()
        {
            this.sdk?.Dispose();
        }

        protected override async void OnStart()
        {
            AppCenter.Start(
                "android=972ae016-29c4-4e4f-af9a-ad7eebfca1f7;uwp={Your UWP App secret here};ios={Your iOS App secret here}",
                typeof(Analytics),
                typeof(Crashes));

            try
            {
                await PerformStartup(false);
            }
            catch (Exception e)
            {
                DisplayErrorPage("PerformStartup", e.ToString());
            }
        }

        protected override async void OnResume()
        {
            await PerformStartup(true);
        }

        protected override async void OnSleep()
        {
            await PerformShutdown();
        }

		private async Task PerformStartup(bool isResuming)
        {
            await this.sdk.Startup(isResuming);
            
            await this.contractOrchestratorService.Load(isResuming);
            await this.navigationOrchestratorService.Load(isResuming);

            this.autoSaveTimer = new Timer(_ => AutoSave(), null, Constants.Intervals.AutoSave, Constants.Intervals.AutoSave);
        }

        private async Task PerformShutdown()
        {
            if (this.autoSaveTimer != null)
            {
                this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.autoSaveTimer.Dispose();
                this.autoSaveTimer = null;
            }
            AutoSave();
            if (MainPage?.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            if (!keepRunningInTheBackground)
            {
                await this.navigationOrchestratorService.Unload();
                await this.contractOrchestratorService.Unload();
            }
            await this.sdk.Shutdown(keepRunningInTheBackground);
        }

        private void AutoSave()
        {
            this.sdk.AutoSave();
        }
    }
}
