using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Extensions;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.Views;
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

            // Start page
            try
            {
                this.MainPage = new AppShell();
                //NavigationPage navigationPage = new NavigationPage(new InitPage())
                //{
                //    BarBackgroundColor = (Color) Current.Resources["HeadingBackground"],
                //    BarTextColor = (Color) Current.Resources["HeadingForeground"]
                //};

                //this.MainPage = navigationPage;
            }
            catch (Exception e)
            {
                WriteExceptionToFile("StartPage", e.ToString());
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
            await SendErrorReportFromPreviousRun();

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

        #region Error Handling

        private const string StartupCrashFileName = "CrashDump.txt";

        private void DisplayErrorPage(string title, string stackTrace)
        {
            WriteExceptionToFile(title, stackTrace);

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

        private void WriteExceptionToFile(string title, string stackTrace)
        {
            string contents;
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StartupCrashFileName);
            if (File.Exists(fileName))
            {
                contents = File.ReadAllText(fileName);
            }
            else
            {
                contents = string.Empty;
            }

            File.WriteAllText(fileName, $"{title}{Environment.NewLine}{stackTrace}{Environment.NewLine}{contents}");
        }

        private string ReadExceptionFromFile()
        {
            string contents;
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StartupCrashFileName);
            if (File.Exists(fileName))
            {
                contents = File.ReadAllText(fileName);
            }
            else
            {
                contents = string.Empty;
            }

            return contents;
        }

        private void DeleteExceptionFile()
        {
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StartupCrashFileName);
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        private async Task SendErrorReportFromPreviousRun()
        {
            string stackTrace = ReadExceptionFromFile();
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
                    DeleteExceptionFile();
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
                WriteExceptionToFile(title, ex.ToString());
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
