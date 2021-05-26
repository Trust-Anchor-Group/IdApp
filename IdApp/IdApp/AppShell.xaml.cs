using System;
using System.Text;
using System.Threading.Tasks;
using IdApp.Services;
using IdApp.ViewModels;
using IdApp.Views;
using IdApp.Views.Contracts;
using IdApp.Views.Identity;
using IdApp.Views.Contacts;
using IdApp.Views.Registration;
using IdApp.Views.Things;
using IdApp.Views.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.IoTGateway.Setup;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp
{
	/// <summary>
	/// The Xamarin Forms Shell implementation of the TAG ID App.
	/// </summary>
	public partial class AppShell
	{
		/// <summary>
		/// Create a new instance of the <see cref="AppShell"/> class.
		/// </summary>
		public AppShell()
		{
			this.ViewModel = new AppShellViewModel();
			InitializeComponent();
			SetTabBarIsVisible(this, false);
			RegisterRoutes();
		}

		/// <summary>
		/// Current Neuron Service
		/// </summary>
		public INeuronService NeuronService => Types.Instantiate<INeuronService>(false);

		/// <summary>
		/// Current Network Service
		/// </summary>
		public INetworkService NetworkService => Types.Instantiate<INetworkService>(false);

		/// <summary>
		/// Current Navigation Service
		/// </summary>
		public INavigationService NavigationService => Types.Instantiate<INavigationService>(false);

		/// <summary>
		/// Current Log Service
		/// </summary>
		public ILogService LogService => Types.Instantiate<ILogService>(false);

		/// <summary>
		/// Current UI Dispatcher Service
		/// </summary>
		public IUiDispatcher UiDispatcher => Types.Instantiate<IUiDispatcher>(false);

		/// <summary>
		/// Current Contract Orchestrator Service
		/// </summary>
		public IContractOrchestratorService ContractOrchestratorService => Types.Instantiate<IContractOrchestratorService>(false);

		/// <summary>
		/// Current Thing Registry Orchestrator Service
		/// </summary>
		public IThingRegistryOrchestratorService ThingRegistryOrchestratorService => Types.Instantiate<IThingRegistryOrchestratorService>(false);

		/// <summary>
		/// Current eDaler Orchestrator Service
		/// </summary>
		public IEDalerOrchestratorService EDalerOrchestratorService => Types.Instantiate<IEDalerOrchestratorService>(false);

		/// <inheritdoc/>
		protected override void OnNavigated(ShellNavigatedEventArgs e)
		{
			base.OnNavigated(e);

			// Once MainPage is shown, hide the "Loading..." Flyout, as the user shouldn't be
			// able to navigate to that page.

			if (e.Current.Location.ToString().Contains(nameof(MainPage)))
				this.LoadingFlyout.IsVisible = false;
		}

		private void RegisterRoutes()
		{
			// Onboarding:
			Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
			Routing.RegisterRoute(nameof(XmppCommunicationPage), typeof(XmppCommunicationPage));

			// General:
			Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));

			// Identity:
			Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
			Routing.RegisterRoute(nameof(PetitionIdentityPage), typeof(PetitionIdentityPage));
			Routing.RegisterRoute(nameof(PetitionSignaturePage), typeof(PetitionSignaturePage));

			// Contacts:
			Routing.RegisterRoute(nameof(MyContactsPage), typeof(MyContactsPage));

			// Contracts:
			Routing.RegisterRoute(nameof(MyContractsPage), typeof(MyContractsPage));
			Routing.RegisterRoute(nameof(SignedContractsPage), typeof(SignedContractsPage));
			Routing.RegisterRoute(nameof(ContractTemplatesPage), typeof(ContractTemplatesPage));
			Routing.RegisterRoute(nameof(NewContractPage), typeof(NewContractPage));
			Routing.RegisterRoute(nameof(ViewContractPage), typeof(ViewContractPage));
			Routing.RegisterRoute(nameof(ClientSignaturePage), typeof(ClientSignaturePage));
			Routing.RegisterRoute(nameof(ServerSignaturePage), typeof(ServerSignaturePage));
			Routing.RegisterRoute(nameof(PetitionContractPage), typeof(PetitionContractPage));

			// Things
			Routing.RegisterRoute(nameof(ViewClaimThingPage), typeof(ViewClaimThingPage));
			Routing.RegisterRoute(nameof(MyThingsPage), typeof(MyThingsPage));
			Routing.RegisterRoute(nameof(ViewThingPage), typeof(ViewThingPage));

			// Wallet
			Routing.RegisterRoute(nameof(IssueEDalerPage), typeof(IssueEDalerPage));
			Routing.RegisterRoute(nameof(EDalerReceivedPage), typeof(EDalerReceivedPage));
			Routing.RegisterRoute(nameof(MyWalletPage), typeof(MyWalletPage));
			Routing.RegisterRoute(nameof(RequestPaymentPage), typeof(RequestPaymentPage));
			Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));
			Routing.RegisterRoute(nameof(PaymentAcceptancePage), typeof(PaymentAcceptancePage));
			Routing.RegisterRoute(nameof(PendingPaymentPage), typeof(PendingPaymentPage));
			Routing.RegisterRoute(nameof(AccountEventPage), typeof(AccountEventPage));
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
				await this.NavigationService?.GoToAsync(route);
			}
		}

		private async void ViewIdentityMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(ViewIdentityPage));
		}

		internal async void ScanQrCodeMenuItem_Clicked(object sender, EventArgs e)
		{
            await QrCode.ScanQrCodeAndHandleResult(this.LogService, this.NeuronService, this.NavigationService,
                this.UiDispatcher, this.ContractOrchestratorService, this.ThingRegistryOrchestratorService,
				this.EDalerOrchestratorService);
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
			await this.GoToPage(nameof(ContractTemplatesPage));
		}

		private async void DebugMenuItem_Clicked(object sender, EventArgs e)
		{
			try
			{
				await App.EvaluateDatabaseDiff("Debug.xml", true, false);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}

			await this.GoToPage(nameof(XmppCommunicationPage));
		}

		private void ExitMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			// Break the call chain by 'posting' to the main thread, allowing the fly out menu to hide before initiating the login/out.
			this.UiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				await App.Stop();
			});
		}

		private void AboutMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			this.UiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Name: {AppInfo.Name}");
				sb.AppendLine($"Version: {AppInfo.VersionString}");
				sb.AppendLine($"Build date: {XmppConfiguration.BuildDate}");
				sb.AppendLine($"Build time: {XmppConfiguration.BuildTime}");
				sb.AppendLine($"Platform: {Device.RuntimePlatform}");
				sb.AppendLine($"RuntimeVersion: {GetType().Assembly.ImageRuntimeVersion}");
				sb.AppendLine($"Phone: {DeviceInfo.Manufacturer} {DeviceInfo.Model}");
				await this.UiDispatcher.DisplayAlert(AppResources.About, sb.ToString());
			});
		}

		internal async void ContactsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContactsPage));
		}

		internal async void ThingsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyThingsPage));
		}

	}
}
