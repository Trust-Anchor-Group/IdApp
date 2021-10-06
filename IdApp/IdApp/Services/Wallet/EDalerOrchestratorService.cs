using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.EDalerReceived;
using IdApp.Pages.Wallet.IssueEDaler;
using IdApp.Pages.Wallet.MyWallet;
using IdApp.Pages.Wallet.Payment;
using IdApp.Pages.Wallet.PaymentAcceptance;
using Waher.Runtime.Inventory;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;

namespace IdApp.Services.Wallet
{
	[Singleton]
	internal class EDalerOrchestratorService : LoadableService, IEDalerOrchestratorService
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiSerializer uiSerializer;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly INetworkService networkService;
		private readonly ISettingsService settingsService;

		public EDalerOrchestratorService(
			ITagProfile tagProfile,
			IUiSerializer uiSerializer,
			INeuronService neuronService,
			INavigationService navigationService,
			ILogService logService,
			INetworkService networkService,
			ISettingsService settingsService)
		{
			this.tagProfile = tagProfile;
			this.uiSerializer = uiSerializer;
			this.neuronService = neuronService;
			this.navigationService = navigationService;
			this.logService = logService;
			this.networkService = networkService;
			this.settingsService = settingsService;
		}

		public override Task Load(bool isResuming)
		{
			if (this.BeginLoad())
			{
				this.neuronService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;
				this.EndLoad(true);
			}
			
			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.neuronService.Wallet.BalanceUpdated -= Wallet_BalanceUpdated;
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private Task Wallet_BalanceUpdated(object Sender, EDaler.BalanceEventArgs e)
		{
			this.uiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if ((e.Balance.Event?.Change ?? 0) > 0)
					await this.navigationService.GoToAsync(nameof(EDalerReceivedPage), new EDalerBalanceNavigationArgs(e.Balance));
				else
					await this.OpenWallet();
			});

			return Task.CompletedTask;
		}

		#endregion

		/// <summary>
		/// Opens the wallet
		/// </summary>
		public async Task OpenWallet()
		{
			try
			{
				Balance Balance = await this.neuronService.Wallet.GetBalanceAsync();
				(decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments) = await this.neuronService.Wallet.GetPendingPayments();
				(AccountEvent[] Events, bool More) = await this.neuronService.Wallet.GetAccountEventsAsync(50);

				WalletNavigationArgs e = new WalletNavigationArgs(Balance, PendingAmount, PendingCurrency, PendingPayments, Events, More);

				await this.navigationService.GoToAsync(nameof(MyWalletPage), e);
			}
			catch (Exception ex)
			{
				await this.uiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// eDaler URI scanned.
		/// </summary>
		/// <param name="uri">eDaler URI.</param>
		public async Task OpenEDalerUri(string uri)
		{
			if (!this.neuronService.Wallet.TryParseEDalerUri(uri, out EDalerUri Parsed, out string Reason))
			{
				await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.InvalidEDalerUri, Reason));
				return;
			}

			if (Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ExpiredEDalerUri);
				return;
			}

			if (Parsed is EDalerIssuerUri)
			{
				this.uiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(IssueEDalerPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerDestroyerUri)
			{
				// TODO
			}
			else if (Parsed is EDalerPaymentUri)
			{
				this.uiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(PaymentAcceptancePage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerIncompletePaymeUri)
			{
				this.uiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else
			{
				await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnrecognizedEDalerURI);
				return;
			}
		}

	}
}