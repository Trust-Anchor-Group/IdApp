using System;
using System.Threading;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.EDalerReceived;
using IdApp.Pages.Wallet.IssueEDaler;
using IdApp.Pages.Wallet.MyWallet;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using IdApp.Pages.Wallet.Payment;
using IdApp.Pages.Wallet.PaymentAcceptance;
using IdApp.Pages.Wallet.TokenDetails;
using IdApp.Resx;
using IdApp.Services.Notification;
using IdApp.Services.Notification.Wallet;
using NeuroFeatures;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Wallet
{
	[Singleton]
	internal class NeuroWalletOrchestratorService : LoadableService, INeuroWalletOrchestratorService
	{
		public NeuroWalletOrchestratorService()
		{
		}

		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (this.BeginLoad(cancellationToken))
			{
				this.XmppService.Wallet.BalanceUpdated += this.Wallet_BalanceUpdated;
				this.XmppService.Wallet.TokenAdded += this.Wallet_TokenAdded;
				this.XmppService.Wallet.TokenRemoved += this.Wallet_TokenRemoved;
				this.XmppService.Wallet.StateUpdated += this.Wallet_StateUpdated;
				this.XmppService.Wallet.VariablesUpdated += this.Wallet_VariablesUpdated;

				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.XmppService.Wallet.BalanceUpdated -= this.Wallet_BalanceUpdated;
				this.XmppService.Wallet.TokenAdded -= this.Wallet_TokenAdded;
				this.XmppService.Wallet.TokenRemoved -= this.Wallet_TokenRemoved;
				this.XmppService.Wallet.StateUpdated -= this.Wallet_StateUpdated;
				this.XmppService.Wallet.VariablesUpdated -= this.Wallet_VariablesUpdated;

				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private async Task Wallet_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			await this.NotificationService.NewEvent(new BalanceNotificationEvent(e));
		}

		private async Task Wallet_TokenAdded(object Sender, TokenEventArgs e)
		{
			await this.NotificationService.NewEvent(new TokenAddedNotificationEvent(e));
		}

		private async Task Wallet_TokenRemoved(object Sender, TokenEventArgs e)
		{
			await this.NotificationService.NewEvent(new TokenRemovedNotificationEvent(e));
		}

		private async Task Wallet_StateUpdated(object Sender, NewStateEventArgs e)
		{
			await this.NotificationService.NewEvent(new StateMachineNewStateNotificationEvent(e));
		}

		private async Task Wallet_VariablesUpdated(object Sender, VariablesUpdatedEventArgs e)
		{
			await this.NotificationService.NewEvent(new StateMachineVariablesUpdatedNotificationEvent(e));
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
				(AccountEvent[] Events, bool More) = await this.XmppService.Wallet.GetAccountEventsAsync(Constants.BatchSizes.AccountEventBatchSize);

				WalletNavigationArgs e = new(Balance, PendingAmount, PendingCurrency, PendingPayments, Events, More);

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
		/// <param name="Uri">eDaler URI.</param>
		public async Task OpenEDalerUri(string Uri)
		{
			if (!this.XmppService.Wallet.TryParseEDalerUri(Uri, out EDalerUri Parsed, out string Reason))
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
			else if (Parsed is EDalerIncompletePaymentUri)
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

		/// <summary>
		/// Neuro-Feature URI scanned.
		/// </summary>
		/// <param name="Uri">Neuro-Feature URI.</param>
		public async Task OpenNeuroFeatureUri(string Uri)
		{
			int i = Uri.IndexOf(':');
			if (i < 0)
				return;

			string TokenId = Uri[(i + 1)..];

			try
			{
				Token Token = await this.XmppService.Wallet.NeuroFeaturesClient.GetTokenAsync(TokenId);

				if (!this.NotificationService.TryGetNotificationEvents(EventButton.Wallet, TokenId, out NotificationEvent[] Events))
					Events = new NotificationEvent[0];

				await this.NavigationService.GoToAsync(nameof(TokenDetailsPage),
					new TokenDetailsNavigationArgs(new TokenItem(Token, this, Events)) { ReturnCounter = 1 });
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex);
			}
		}

	}
}
