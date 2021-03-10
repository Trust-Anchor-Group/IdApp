using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdApp.Services;
using IdApp.ViewModels;
using IdApp.Views;
using IdApp.Views.Contracts;
using IdApp.Views.Registration;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.IoTGateway.Setup;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp
{
	/// <summary>
	/// The Xamarin Forms Shell implementation of the TAG ID App.
	/// </summary>
	public partial class AppShell
	{
		private readonly INeuronService neuronService;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly IUiDispatcher uiDispatcher;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly IThingRegistryOrchestratorService thingRegistryOrchestratorService;
		private static AppShell _instance;
		internal static AppShell Instance => _instance ?? (_instance = new AppShell());

		/// <summary>
		/// Create a new instance of the <see cref="AppShell"/> class.
		/// </summary>
		public AppShell()
		{
			this.ViewModel = new AppShellViewModel();
			this.neuronService = DependencyService.Resolve<INeuronService>();
			this.networkService = DependencyService.Resolve<INetworkService>();
			this.navigationService = DependencyService.Resolve<INavigationService>();
			this.logService = DependencyService.Resolve<ILogService>();
			this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
			this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
			this.thingRegistryOrchestratorService = DependencyService.Resolve<IThingRegistryOrchestratorService>();
			InitializeComponent();
			SetTabBarIsVisible(this, false);
			RegisterRoutes();
		}

		/// <inheritdoc/>
		protected override void OnNavigated(ShellNavigatedEventArgs e)
		{
			base.OnNavigated(e);
			// Once MainPage is shown, hide the "Loading..." Flyout, as the user shouldn't be
			// able to navigate to that page.
			if (e.Current.Location.ToString().Contains(nameof(MainPage)))
			{
				this.LoadingFlyout.IsVisible = false;
			}
		}

		private void RegisterRoutes()
		{
			Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
			Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
			Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
			Routing.RegisterRoute(nameof(MyContractsPage), typeof(MyContractsPage));
			Routing.RegisterRoute(nameof(SignedContractsPage), typeof(SignedContractsPage));
			Routing.RegisterRoute(nameof(NewContractPage), typeof(NewContractPage));
			Routing.RegisterRoute(nameof(ViewContractPage), typeof(ViewContractPage));
			Routing.RegisterRoute(nameof(ClientSignaturePage), typeof(ClientSignaturePage));
			Routing.RegisterRoute(nameof(ServerSignaturePage), typeof(ServerSignaturePage));
			Routing.RegisterRoute(nameof(PetitionContractPage), typeof(PetitionContractPage));
			Routing.RegisterRoute(nameof(PetitionIdentityPage), typeof(PetitionIdentityPage));
			Routing.RegisterRoute(nameof(PetitionSignaturePage), typeof(PetitionSignaturePage));
			Routing.RegisterRoute(nameof(XmppCommunicationPage), typeof(XmppCommunicationPage));
		}

		private async Task GoToPage(string route)
		{
			// Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
			// Therefore we have to close it manually here.
			Current.FlyoutIsPresented = false;

			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.
			if (this.GetViewModel<AppShellViewModel>().IsConnected)
			{
				await this.navigationService.GoToAsync(route);
			}
		}

		private async void ViewIdentityMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(ViewIdentityPage));
		}

		internal async void ScanQrCodeMenuItem_Clicked(object sender, EventArgs e)
		{
			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.
			if (!this.GetViewModel<AppShellViewModel>().IsConnected)
			{
				return;
			}

			string DecodedText = await QrCode.ScanQrCode(this.navigationService, AppResources.Open);

			if (string.IsNullOrWhiteSpace(DecodedText))
				return;

			try
			{
				if (!Uri.TryCreate(DecodedText, UriKind.Absolute, out Uri uri))
				{
					await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.CodeNotRecognized);
					return;
				}

				switch (uri.Scheme.ToLower())
				{
					case Constants.UriSchemes.UriSchemeIotId:
						string legalId = Constants.UriSchemes.GetCode(DecodedText);
						await this.contractOrchestratorService.OpenLegalIdentity(legalId, AppResources.ScannedQrCode);
						break;

					case Constants.UriSchemes.UriSchemeIotSc:
						string contractId = Constants.UriSchemes.GetCode(DecodedText);
						await this.contractOrchestratorService.OpenContract(contractId, AppResources.ScannedQrCode);
						break;

					case Constants.UriSchemes.UriSchemeIotDisco:
						if (this.neuronService.ThingRegistry.IsIoTDiscoClaimURI(DecodedText))
							await this.thingRegistryOrchestratorService.OpenClaimDevice(DecodedText);
						else if (this.neuronService.ThingRegistry.IsIoTDiscoSearchURI(DecodedText))
							await this.thingRegistryOrchestratorService.OpenSearchDevices(DecodedText);
						else
							await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, $"{AppResources.InvalidIoTDiscoveryCode}{Environment.NewLine}{Environment.NewLine}{DecodedText}");
						break;

					case Constants.UriSchemes.UriSchemeTagSign:
						string request = Constants.UriSchemes.GetCode(DecodedText);
						await this.contractOrchestratorService.TagSignature(request);
						break;

					default:
						if (!await Launcher.TryOpenAsync(uri))
							await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, $"{AppResources.QrCodeNotUnderstood}{Environment.NewLine}{Environment.NewLine}{DecodedText}");
						break;
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private async void MyContractsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContractsPage));
		}

		private async void SignedContractsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(SignedContractsPage));
		}

		private async void NewContractMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(NewContractPage));
		}

		private async void DebugMenuItem_Clicked(object sender, EventArgs e)
		{
			try
			{
				await App.EvaluateDatabaseDiff("Debug.xml", true, false);
			}
			catch (Exception ex)
			{
				logService.LogException(ex);
			}

			await this.GoToPage(nameof(XmppCommunicationPage));
		}

		private void SignInOutMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			// Break the call chain by 'posting' to the main thread, allowing the fly out menu to hide before initiating the login/out.
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				if (!this.networkService.IsOnline)
				{
					await this.uiDispatcher.DisplayAlert(AppResources.AnErrorHasOccurred, AppResources.NetworkSeemsToBeMissing);
					return;
				}

				if (this.neuronService.IsLoggedOut)
				{
					await this.neuronService.LogIn();
				}
				else
				{
					await this.neuronService.LogOut();
				}
			});
		}

		private void AboutMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Name: {AppInfo.Name}");
				sb.AppendLine($"Version: {AppInfo.VersionString}");
				sb.AppendLine($"Build date: {XmppConfiguration.BuildDate}");
				sb.AppendLine($"Build time: {XmppConfiguration.BuildTime}");
				sb.AppendLine($"Platform: {Device.RuntimePlatform}");
				sb.AppendLine($"RuntimeVersion: {GetType().Assembly.ImageRuntimeVersion}");
				sb.AppendLine($"Phone: {DeviceInfo.Manufacturer} {DeviceInfo.Model}");
				await this.uiDispatcher.DisplayAlert(AppResources.About, sb.ToString());
			});
		}
	}
}
