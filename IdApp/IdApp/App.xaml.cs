using IdApp.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Waher.Runtime.Settings;
using Waher.Script;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Device = Xamarin.Forms.Device;

namespace IdApp
{
	/// <summary>
	/// The Application class, representing an instance of the IdApp.
	/// </summary>
	public partial class App
	{
		private readonly ITagIdSdk sdk;
		private readonly IImageCacheService imageCacheService;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly bool keepRunningInTheBackground = false;
		private Profiler startupProfiler;

		///<inheritdoc/>
		public App()
		{
			this.startupProfiler = new Profiler("Startup", ProfilerThreadType.Sequential);	// Comment out, to remove startup profiling.
			this.startupProfiler?.Start();
			this.startupProfiler?.NewState("Init");

			AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			InitializeComponent();

			try
			{
				this.startupProfiler?.NewState("Types");

				Assembly appAssembly = this.GetType().Assembly;

				if (!Types.IsInitialized)
				{
					// Define the scope and reach of Runtime.Inventory (Script, Serialization, Persistence, IoC, etc.):

					Types.Initialize(
						appAssembly,                                // Allows for objects defined in this assembly, to be instantiated and persisted.
						typeof(Database).Assembly,                  // Indexes default attributes
						typeof(ObjectSerializer).Assembly,          // Indexes general serializers
						typeof(FilesProvider).Assembly,             // Indexes special serializers
						typeof(RuntimeSettings).Assembly,           // Allows for persistence of settings in the object database
						typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
						typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
						typeof(Expression).Assembly,                // Indexes basic script functions
						typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
						typeof(TagConfiguration).Assembly,          // Indexes persistable objects
						typeof(RegistrationStep).Assembly);         // Indexes persistable objects
				}

				this.startupProfiler?.NewState("SDK");

				this.sdk = TagIdSdk.Create(appAssembly, this.startupProfiler, new XmppConfiguration().ToArray());

				// Set resolver
				DependencyResolver.ResolveUsing(type =>
				{
					if (Types.GetType(type.FullName) is null)
						return null;    // Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.

					return Types.Instantiate(true, type);
				});

                // Get the db started right away to save startup time.
                this.sdk.StorageService.Init(this.startupProfiler?.CreateThread("Database", ProfilerThreadType.Sequential));

				// Register log listener (optional)
				this.sdk.LogService.AddListener(new AppCenterEventSink(this.sdk.LogService));

				// Resolve what's needed for the App class
				this.imageCacheService = DependencyService.Resolve<IImageCacheService>();
				this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
			}
			catch (Exception e)
			{
				e = Waher.Events.Log.UnnestException(e);
				this.startupProfiler?.Exception(e);
				DisplayBootstrapErrorPage(e.Message, e.StackTrace);
				return;
			}

			// Start page
			try
			{
				this.startupProfiler?.NewState("MainPage");

				this.MainPage = new AppShell();
			}
			catch (Exception e)
			{
				e = Waher.Events.Log.UnnestException(e);
				this.startupProfiler?.Exception(e);
				this.sdk.LogService.SaveExceptionDump("StartPage", e.ToString());
			}

			this.startupProfiler?.MainThread.Idle();
		}

		///<inheritdoc/>
		protected override async void OnStart()
        {
            //AppCenter.Start(
            //    "android={Your Android App secret here};uwp={Your UWP App secret here};ios={Your iOS App secret here}",
            //    typeof(Analytics),
            //    typeof(Crashes));

            await this.PerformStartup(false);
        }

        ///<inheritdoc/>
        protected override async void OnResume()
        {
            await this.PerformStartup(true);
        }

		private async Task PerformStartup(bool isResuming)
		{
			ProfilerThread Thread = this.startupProfiler?.MainThread.CreateSubThread("AppStartup", ProfilerThreadType.Sequential);
			Thread?.Start();

			try
			{
				Thread?.NewState("Report");
				await this.SendErrorReportFromPreviousRun();

				Thread?.NewState("Startup");
				await this.sdk.Startup(isResuming);

				Thread?.NewState("Cache");
				await this.imageCacheService.Load(isResuming);

				Thread?.NewState("Orchestrator");
				await this.contractOrchestratorService.Load(isResuming);
			}
			catch (Exception e)
			{
				e = Waher.Events.Log.UnnestException(e);
				Thread?.Exception(e);
				this.DisplayBootstrapErrorPage(e.Message, e.StackTrace);
			}

			Thread?.Stop();
			this.SendStartupProfile();
		}

		///<inheritdoc/>
		protected override async void OnSleep()
		{
			await this.PerformShutdown();
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
					await this.SendAlert(stackTrace, "text/plain");
				}
				finally
				{
					this.sdk.LogService.DeleteExceptionDump();
				}
			}
		}

		private async Task SendAlert(string Message, string ContentType)
		{
			try
			{
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				var content = new StringContent(Message);
				content.Headers.ContentType.MediaType = ContentType;
				await client.PostAsync("https://lab.tagroot.io/Alert.ws", content);
			}
			catch (Exception ex)
			{
				Waher.Events.Log.Critical(ex);
			}
		}

		private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
		{
			this.startupProfiler?.Exception(e.Exception);
		}

		private void SendStartupProfile()
		{
			if (!(this.startupProfiler is null))
			{
				this.startupProfiler.Stop();
				AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;

				string Uml = this.startupProfiler.ExportPlantUml(TimeUnit.MilliSeconds);

				this.startupProfiler = null;

				Task.Run(async () =>
				{
					try
					{
						await SendAlert("```uml\r\n" + Uml + "```", "text/markdown");
					}
					catch (Exception ex)
					{
						Waher.Events.Log.Critical(ex);
					}
				});
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
