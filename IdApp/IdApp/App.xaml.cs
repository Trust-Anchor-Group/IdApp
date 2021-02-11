using IdApp.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Events;
using Waher.IoTGateway.Setup;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Device = Xamarin.Forms.Device;

namespace IdApp
{
	/// <summary>
	/// The Application class, representing an instance of the IdApp.
	/// </summary>
	public partial class App : IDisposable
	{
		private readonly ITagIdSdk sdk;
		private readonly IImageCacheService imageCacheService;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly bool keepRunningInTheBackground = false;

        ///<inheritdoc/>
		public App()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			InitializeComponent();

			try
            {
                Assembly[] additionalAssemblies = { typeof(RegistrationStep).Assembly };
                this.sdk = TagIdSdk.Create(this.GetType().Assembly, additionalAssemblies, new XmppConfiguration().ToArray());

				// App Registrations
				//this.container.Register<IImageCacheService, ImageCacheService>(IocScope.Singleton);
				//this.container.Register<IContractOrchestratorService, ContractOrchestratorService>(IocScope.Singleton);

				// Set resolver
				DependencyResolver.ResolveUsing(type =>
				{
					object Result = Types.Instantiate(true, type);
					if (!(Result is null))
						return Result;

					throw new InvalidOperationException("Unable to instantiate " + type.FullName);
				});

				// Register log listener (optional)
				this.sdk.LogService.AddListener(new AppCenterLogListener());

				// Resolve what's needed for the App class
				this.imageCacheService = DependencyService.Resolve<IImageCacheService>();
				this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
			}
			catch (Exception e)
			{
				e = Waher.Events.Log.UnnestException(e);
				DisplayBootstrapErrorPage(e.Message, e.StackTrace);
				return;
			}

			// Start page
			try
			{
				this.MainPage = new AppShell();
			}
			catch (Exception e)
			{
				e = Waher.Events.Log.UnnestException(e);
				this.sdk.LogService.SaveExceptionDump("StartPage", e.ToString());
			}
		}

        ///<inheritdoc/>
        public void Dispose()
		{
			this.sdk?.Dispose();
		}

        ///<inheritdoc/>
		protected override async void OnStart()
		{
			//AppCenter.Start(
			//    "android={Your Android App secret here};uwp={Your UWP App secret here};ios={Your iOS App secret here}",
			//    typeof(Analytics),
			//    typeof(Crashes));

			try
			{
				await this.PerformStartup(false);
			}
			catch (Exception e)
			{
				e = Waher.Events.Log.UnnestException(e);
				this.DisplayBootstrapErrorPage(e.Message, e.StackTrace);
			}
		}

        ///<inheritdoc/>
		protected override async void OnResume()
		{
			await this.PerformStartup(true);
		}

        ///<inheritdoc/>
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
		}

		private async Task PerformShutdown()
		{
			// Done manually here, as the Disappearing event won't trigger when exiting the app,
			// and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.
			if (MainPage?.BindingContext is BaseViewModel vm)
			{
				await vm.Shutdown();
			}

			if (!keepRunningInTheBackground)
			{
				await this.contractOrchestratorService.Unload();
			}
			await this.sdk.Shutdown(keepRunningInTheBackground);
		}

		#region Error Handling

		private void DisplayBootstrapErrorPage(string title, string stackTrace)
		{
			this.sdk?.LogService?.SaveExceptionDump(title, stackTrace);

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
