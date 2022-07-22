using System;
using System.Text;
using System.Threading.Tasks;
using IdApp.Pages.Contracts.MyContracts;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Xmpp;
using IdApp.Services.ThingRegistries;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using IdApp.Services.Wallet;
using Xamarin.Essentials;
using Xamarin.Forms;
using IdApp.Resx;

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
			Routing.RegisterRoute(nameof(ScanQrCode.ScanQrCodePage), typeof(ScanQrCode.ScanQrCodePage));

			// Identity:
			Routing.RegisterRoute(nameof(Identity.ViewIdentity.ViewIdentityPage), typeof(Identity.ViewIdentity.ViewIdentityPage));
			Routing.RegisterRoute(nameof(Identity.PetitionIdentity.PetitionIdentityPage), typeof(Identity.PetitionIdentity.PetitionIdentityPage));
			Routing.RegisterRoute(nameof(Identity.TransferIdentity.TransferIdentityPage), typeof(Identity.TransferIdentity.TransferIdentityPage));

			// Contacts:
			Routing.RegisterRoute(nameof(Contacts.MyContacts.MyContactsPage), typeof(Contacts.MyContacts.MyContactsPage));
			Routing.RegisterRoute(nameof(Contacts.Chat.ChatPage),
				Device.RuntimePlatform == Device.iOS ? typeof(Contacts.Chat.ChatPageIos) :
				typeof(Contacts.Chat.ChatPage));

			// Contracts:
			Routing.RegisterRoute(nameof(Contracts.ClientSignature.ClientSignaturePage), typeof(Contracts.ClientSignature.ClientSignaturePage));
			Routing.RegisterRoute(nameof(Contracts.MyContracts.MyContractsPage), typeof(Contracts.MyContracts.MyContractsPage));
			Routing.RegisterRoute(nameof(Contracts.NewContract.NewContractPage), typeof(Contracts.NewContract.NewContractPage));
			Routing.RegisterRoute(nameof(Contracts.PetitionContract.PetitionContractPage), typeof(Contracts.PetitionContract.PetitionContractPage));
			Routing.RegisterRoute(nameof(Contracts.PetitionSignature.PetitionSignaturePage), typeof(Contracts.PetitionSignature.PetitionSignaturePage));
			Routing.RegisterRoute(nameof(Contracts.ServerSignature.ServerSignaturePage), typeof(Contracts.ServerSignature.ServerSignaturePage));
			Routing.RegisterRoute(nameof(Contracts.ViewContract.ViewContractPage), typeof(Contracts.ViewContract.ViewContractPage));

			// Things
			Routing.RegisterRoute(nameof(Things.ViewClaimThing.ViewClaimThingPage), typeof(Things.ViewClaimThing.ViewClaimThingPage));
			Routing.RegisterRoute(nameof(Things.MyThings.MyThingsPage), typeof(Things.MyThings.MyThingsPage));
			Routing.RegisterRoute(nameof(Things.ViewThing.ViewThingPage), typeof(Things.ViewThing.ViewThingPage));

			// Wallet
			Routing.RegisterRoute(nameof(Wallet.IssueEDaler.IssueEDalerPage), typeof(Wallet.IssueEDaler.IssueEDalerPage));
			Routing.RegisterRoute(nameof(Wallet.EDalerReceived.EDalerReceivedPage), typeof(Wallet.EDalerReceived.EDalerReceivedPage));
			Routing.RegisterRoute(nameof(Wallet.MyWallet.MyWalletPage), typeof(Wallet.MyWallet.MyWalletPage));
			Routing.RegisterRoute(nameof(Wallet.RequestPayment.RequestPaymentPage), typeof(Wallet.RequestPayment.RequestPaymentPage));
			Routing.RegisterRoute(nameof(Wallet.Payment.PaymentPage), typeof(Wallet.Payment.PaymentPage));
			Routing.RegisterRoute(nameof(Wallet.SendPayment.SendPaymentPage), typeof(Wallet.SendPayment.SendPaymentPage));
			Routing.RegisterRoute(nameof(Wallet.PaymentAcceptance.PaymentAcceptancePage), typeof(Wallet.PaymentAcceptance.PaymentAcceptancePage));
			Routing.RegisterRoute(nameof(Wallet.PendingPayment.PendingPaymentPage), typeof(Wallet.PendingPayment.PendingPaymentPage));
			Routing.RegisterRoute(nameof(Wallet.AccountEvent.AccountEventPage), typeof(Wallet.AccountEvent.AccountEventPage));
			Routing.RegisterRoute(nameof(Wallet.TokenDetails.TokenDetailsPage), typeof(Wallet.TokenDetails.TokenDetailsPage));
			Routing.RegisterRoute(nameof(Wallet.TokenEvents.TokenEventsPage), typeof(Wallet.TokenEvents.TokenEventsPage));
			Routing.RegisterRoute(nameof(Wallet.MyTokens.MyTokensPage), typeof(Wallet.MyTokens.MyTokensPage));
			Routing.RegisterRoute(nameof(Wallet.MachineVariables.MachineVariablesPage), typeof(Wallet.MachineVariables.MachineVariablesPage));
			Routing.RegisterRoute(nameof(Wallet.MachineReport.MachineReportPage), typeof(Wallet.MachineReport.MachineReportPage));
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

		private async void ViewIdentityMenuItem_Clicked(object sender, EventArgs e)
		{
			if (!await App.VerifyPin())
				return;

			await this.GoToPage(nameof(Identity.ViewIdentity.ViewIdentityPage));
		}

		internal async void ScanQrCodeMenuItem_Clicked(object sender, EventArgs e)
		{
			await QrCode.ScanQrCodeAndHandleResult();
		}

		private async void MyContractsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.MyContracts));
		}

		private async void SignedContractsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.SignedContracts));
		}

		private async void NewContractMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.ContractTemplates));
		}

		private void ExitMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			// Break the call chain by 'posting' to the main thread, allowing the fly out menu to hide before initiating the login/out.
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				await App.Stop();
			});
		}

		private void AboutMenuItem_Clicked(object sender, EventArgs e)
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

				await this.UiSerializer.DisplayAlert(AppResources.About, sb.ToString());
			});
		}

		internal async void ContactsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Contacts.MyContacts.MyContactsPage),
				new Contacts.ContactListNavigationArgs(AppResources.ContactsDescription, Contacts.SelectContactAction.ViewIdentity));
		}

		internal async void ThingsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Things.MyThings.MyThingsPage));
		}

	}
}
