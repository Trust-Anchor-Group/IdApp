using System;
using System.Text;
using System.Threading.Tasks;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.ClientSignature;
using IdApp.Pages.Contracts.MyContracts;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.PetitionContract;
using IdApp.Pages.Contracts.PetitionSignature;
using IdApp.Pages.Contracts.ServerSignature;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Identity.PetitionIdentity;
using IdApp.Pages.Identity.TransferIdentity;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Pages.Main.Calculator;
using IdApp.Pages.Main.ScanQrCode;
using IdApp.Pages.Main.Security;
using IdApp.Pages.Things.MyThings;
using IdApp.Pages.Things.ReadSensor;
using IdApp.Pages.Things.ViewClaimThing;
using IdApp.Pages.Things.ViewThing;
using IdApp.Pages.Wallet.AccountEvent;
using IdApp.Pages.Wallet.EDalerReceived;
using IdApp.Pages.Wallet.IssueEDaler;
using IdApp.Pages.Wallet.MachineReport;
using IdApp.Pages.Wallet.MachineVariables;
using IdApp.Pages.Wallet.MyTokens;
using IdApp.Pages.Wallet.MyWallet;
using IdApp.Pages.Wallet.Payment;
using IdApp.Pages.Wallet.PaymentAcceptance;
using IdApp.Pages.Wallet.PendingPayment;
using IdApp.Pages.Wallet.RequestPayment;
using IdApp.Pages.Wallet.SendPayment;
using IdApp.Pages.Wallet.ServiceProviders;
using IdApp.Pages.Wallet.TokenDetails;
using IdApp.Pages.Wallet.TokenEvents;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.ThingRegistries;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using IdApp.Services.Wallet;
using IdApp.Services.Xmpp;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Shell
{
	/// <summary>
	/// The Xamarin Forms Shell implementation of the TAG ID App.
	/// </summary>
	public partial class AppShell : ShellBasePage
	{
		/// <summary>
		/// Create a new instance of the <see cref="AppShell"/> class.
		/// </summary>
		public AppShell()
		{
			this.ViewModel = new AppShellViewModel();
			this.InitializeComponent();
			SetTabBarIsVisible(this, false);
			this.RegisterRoutes();
		}

		/// <summary>
		/// Current XMPP Service
		/// </summary>
		public IXmppService XmppService => App.Instantiate<IXmppService>();

		/// <summary>
		/// Current Network Service
		/// </summary>
		public INetworkService NetworkService => App.Instantiate<INetworkService>();

		/// <summary>
		/// Current Navigation Service
		/// </summary>
		public INavigationService NavigationService => App.Instantiate<INavigationService>();

		/// <summary>
		/// Current Log Service
		/// </summary>
		public ILogService LogService => App.Instantiate<ILogService>();

		/// <summary>
		/// Current UI Dispatcher Service
		/// </summary>
		public IUiSerializer UiSerializer => App.Instantiate<IUiSerializer>();

		/// <summary>
		/// Current Contract Orchestrator Service
		/// </summary>
		public IContractOrchestratorService ContractOrchestratorService => App.Instantiate<IContractOrchestratorService>();

		/// <summary>
		/// Current Thing Registry Orchestrator Service
		/// </summary>
		public IThingRegistryOrchestratorService ThingRegistryOrchestratorService => App.Instantiate<IThingRegistryOrchestratorService>();

		/// <summary>
		/// Current Neuro-Wallet Orchestrator Service
		/// </summary>
		public INeuroWalletOrchestratorService NeuroWalletOrchestratorService => App.Instantiate<INeuroWalletOrchestratorService>();

		private void RegisterRoutes()
		{
			// General:
			Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
			Routing.RegisterRoute(nameof(CalculatorPage), typeof(CalculatorPage));
			Routing.RegisterRoute(nameof(SecurityPage), typeof(SecurityPage));

			// Identity:
			Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
			Routing.RegisterRoute(nameof(PetitionIdentityPage), typeof(PetitionIdentityPage));
			Routing.RegisterRoute(nameof(TransferIdentityPage), typeof(TransferIdentityPage));

			// Contacts:
			Routing.RegisterRoute(nameof(MyContactsPage), typeof(MyContactsPage));
			Routing.RegisterRoute(nameof(ChatPage), Device.RuntimePlatform == Device.iOS ? typeof(ChatPageIos) : typeof(ChatPage));

			// Contracts:
			Routing.RegisterRoute(nameof(ClientSignaturePage), typeof(ClientSignaturePage));
			Routing.RegisterRoute(nameof(MyContractsPage), typeof(MyContractsPage));
			Routing.RegisterRoute(nameof(NewContractPage), typeof(NewContractPage));
			Routing.RegisterRoute(nameof(PetitionContractPage), typeof(PetitionContractPage));
			Routing.RegisterRoute(nameof(PetitionSignaturePage), typeof(PetitionSignaturePage));
			Routing.RegisterRoute(nameof(ServerSignaturePage), typeof(ServerSignaturePage));
			Routing.RegisterRoute(nameof(ViewContractPage), typeof(ViewContractPage));

			// Things
			Routing.RegisterRoute(nameof(ViewClaimThingPage), typeof(ViewClaimThingPage));
			Routing.RegisterRoute(nameof(MyThingsPage), typeof(MyThingsPage));
			Routing.RegisterRoute(nameof(ViewThingPage), typeof(ViewThingPage));
			Routing.RegisterRoute(nameof(ReadSensorPage), typeof(ReadSensorPage));

			// Wallet
			Routing.RegisterRoute(nameof(IssueEDalerPage), typeof(IssueEDalerPage));
			Routing.RegisterRoute(nameof(EDalerReceivedPage), typeof(EDalerReceivedPage));
			Routing.RegisterRoute(nameof(MyWalletPage), typeof(MyWalletPage));
			Routing.RegisterRoute(nameof(RequestPaymentPage), typeof(RequestPaymentPage));
			Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));
			Routing.RegisterRoute(nameof(SendPaymentPage), typeof(SendPaymentPage));
			Routing.RegisterRoute(nameof(PaymentAcceptancePage), typeof(PaymentAcceptancePage));
			Routing.RegisterRoute(nameof(PendingPaymentPage), typeof(PendingPaymentPage));
			Routing.RegisterRoute(nameof(AccountEventPage), typeof(AccountEventPage));
			Routing.RegisterRoute(nameof(TokenDetailsPage), typeof(TokenDetailsPage));
			Routing.RegisterRoute(nameof(TokenEventsPage), typeof(TokenEventsPage));
			Routing.RegisterRoute(nameof(MyTokensPage), typeof(MyTokensPage));
			Routing.RegisterRoute(nameof(MachineVariablesPage), typeof(MachineVariablesPage));
			Routing.RegisterRoute(nameof(MachineReportPage), typeof(MachineReportPage));
			Routing.RegisterRoute(nameof(ServiceProvidersPage), typeof(ServiceProvidersPage));
		}

		private async Task GoToPage(string route)
		{
			// Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
			// Therefore we have to close it manually here.

			Current.FlyoutIsPresented = false;

			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.

			if (this.GetViewModel<AppShellViewModel>().IsConnected)
				await this.NavigationService?.GoToAsync(route);
		}

		private async Task GoToPage<TArgs>(string route, TArgs e)
			where TArgs : NavigationArgs, new()
		{
			// Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
			// Therefore we have to close it manually here.

			Current.FlyoutIsPresented = false;

			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.

			if (this.GetViewModel<AppShellViewModel>().IsConnected)
				await this.NavigationService.GoToAsync<TArgs>(route, e);
		}

		private async void ViewIdentityMenuItem_Clicked(object Sender, EventArgs e)
		{
			if (!await App.VerifyPin())
				return;

			await this.GoToPage(nameof(ViewIdentityPage));
		}

		internal async void ScanQrCodeMenuItem_Clicked(object Sender, EventArgs e)
		{
			await QrCode.ScanQrCodeAndHandleResult();
		}

		private async void ContractsMenuItem_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.Contracts));
		}

		private async void NewContractMenuItem_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.ContractTemplates));
		}

		private async void Calculator_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(CalculatorPage), new CalculatorNavigationArgs(null));
		}

		private async void Security_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(SecurityPage), new NavigationArgs());
		}

		private void ExitMenuItem_Clicked(object Sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			// Break the call chain by 'posting' to the main thread, allowing the fly out menu to hide before initiating the login/out.
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				await App.Stop();
			});
		}

		private void AboutMenuItem_Clicked(object Sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				StringBuilder sb = new();

				sb.AppendLine("Name: " + AppInfo.Name);
				sb.AppendLine("Version: " + AppInfo.VersionString + "." + AppInfo.BuildString);
				sb.AppendLine("Runtime: " + this.GetType().Assembly.ImageRuntimeVersion);
				sb.AppendLine("Manufacturer: " + DeviceInfo.Manufacturer);
				sb.AppendLine("Phone: " + DeviceInfo.Model);
				sb.AppendLine("Platform: " + DeviceInfo.Platform + " " + DeviceInfo.VersionString);

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["About"], sb.ToString());
			});
		}

		internal async void ContactsMenuItem_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContactsPage), new ContactListNavigationArgs(LocalizationResourceManager.Current["ContactsDescription"],
				SelectContactAction.ViewIdentity));
		}

		internal async void ThingsMenuItem_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(Things.MyThings.MyThingsPage));
		}

	}
}
