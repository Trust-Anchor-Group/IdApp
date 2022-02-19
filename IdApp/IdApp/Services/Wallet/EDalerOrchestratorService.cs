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
using IdApp.Resx;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Wallet
{
	[Singleton]
	internal class EDalerOrchestratorService : LoadableService, IEDalerOrchestratorService
	{
		public EDalerOrchestratorService()
		{
		}

		public override Task Load(bool isResuming)
		{
			if (this.BeginLoad())
			{
				this.XmppService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;
				this.EndLoad(true);
			}
			
			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.XmppService.Wallet.BalanceUpdated -= Wallet_BalanceUpdated;
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private Task Wallet_BalanceUpdated(object Sender, EDaler.BalanceEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if ((e.Balance.Event?.Change ?? 0) > 0)
					await this.NavigationService.GoToAsync(nameof(EDalerReceivedPage), new EDalerBalanceNavigationArgs(e.Balance));
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
				Balance Balance = await this.XmppService.Wallet.GetBalanceAsync();
				(decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments) = await this.XmppService.Wallet.GetPendingPayments();
				(AccountEvent[] Events, bool More) = await this.XmppService.Wallet.GetAccountEventsAsync(50);

				WalletNavigationArgs e = new WalletNavigationArgs(Balance, PendingAmount, PendingCurrency, PendingPayments, Events, More);

				await this.NavigationService.GoToAsync(nameof(MyWalletPage), e);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// eDaler URI scanned.
		/// </summary>
		/// <param name="uri">eDaler URI.</param>
		public async Task OpenEDalerUri(string uri)
		{
			if (!this.XmppService.Wallet.TryParseEDalerUri(uri, out EDalerUri Parsed, out string Reason))
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.InvalidEDalerUri, Reason));
				return;
			}

			if (Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ExpiredEDalerUri);
				return;
			}

			if (Parsed is EDalerIssuerUri)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(IssueEDalerPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerDestroyerUri)
			{
				// TODO
			}
			else if (Parsed is EDalerPaymentUri)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(PaymentAcceptancePage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerIncompletePaymeUri)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnrecognizedEDalerURI);
				return;
			}
		}

	}
}