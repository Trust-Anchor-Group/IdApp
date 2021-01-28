using Autofac;
using IdApp.Services;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Device = Xamarin.Forms.Device;

namespace IdApp
{
    public partial class App : IDisposable
    {
        private readonly ITagIdSdk sdk;
        private readonly IImageCacheService imageCacheService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly INavigationOrchestratorService navigationOrchestratorService;
        private readonly bool keepRunningInTheBackground = false;

        public App()
		{
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            InitializeComponent();

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
                builder.RegisterType<ImageCacheService>().As<IImageCacheService>().SingleInstance();
                builder.RegisterType<ContractOrchestratorService>().As<IContractOrchestratorService>().SingleInstance();
                builder.RegisterType<NavigationOrchestratorService>().As<INavigationOrchestratorService>().SingleInstance();
                IContainer container = builder.Build();
                // Set AutoFac to be the dependency resolver
                DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

                // Resolve what's needed for the App class
                this.imageCacheService = DependencyService.Resolve<IImageCacheService>();
                this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
                this.navigationOrchestratorService = DependencyService.Resolve<INavigationOrchestratorService>();
            }
            catch (Exception e)
            {
                DisplayErrorPage("ContainerBuilder", e.ToString());
                return;
            }

            // Start page
            try
            {
                this.MainPage = new AppShell();
            }
            catch (Exception e)
            {
                this.sdk.LogService.SaveExceptionDump("StartPage", e.ToString());
                return;
            }
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
                await this.PerformStartup(false);
            }
            catch (Exception e)
            {
                this.DisplayErrorPage("PerformStartup", e.ToString());
            }
        }

        protected override async void OnResume()
        {
            await this.PerformStartup(true);
        }

        protected override async void OnSleep()
        {
            await this.PerformShutdown();
        }

		private async Task PerformStartup(bool isResuming)
        {
            await this.SendErrorReportFromPreviousRun();

            await this.sdk.Startup(isResuming);

            await this.imageCacheService.Load(isResuming);
            await this.contractOrchestratorService.Load(isResuming);
            await this.navigationOrchestratorService.Load(isResuming);
        }

        private async Task PerformShutdown()
        {
            // Done manually here, as the Disappearing event won't trigger when exiting the app,
            // and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.
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

        #region Error Handling

        private void DisplayErrorPage(string title, string stackTrace)
        {
            this.sdk.LogService.SaveExceptionDump(title, stackTrace);

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
            string stackTrace = this.sdk.LogService.LoadExceptionDump();
            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                try
                {
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(stackTrace);
                    content.Headers.ContentType.MediaType = "text/plain";
                    await client.PostAsync("https://lab.tagroot.io/Alert.ws", content);
                }
                catch (Exception)
                {
                }
                finally
                {
                    this.sdk.LogService.DeleteExceptionDump();
                }
            }
        }

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
                this.sdk.LogService.SaveExceptionDump(title, ex.ToString());
            }

            if (ex != null)
            {
                sdk?.LogService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
            }

            if (this.sdk != null && shutdown)
            {
                await this.sdk.ShutdownInPanic();
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

        #endregion
    }
}
