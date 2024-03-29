﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.IssueEDaler;
using IdApp.Pages.Wallet.MyWallet;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using IdApp.Pages.Wallet.Payment;
using IdApp.Pages.Wallet.PaymentAcceptance;
using IdApp.Pages.Wallet.TokenDetails;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using IdApp.Services.Notification.Wallet;
using NeuroFeatures;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

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
				this.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
				this.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
				this.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
				this.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
				this.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;

				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
				this.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
				this.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
				this.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;
				this.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;

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
			if (this.NotificationService.TryGetNotificationEvents(EventButton.Wallet, e.Token.TokenId, out NotificationEvent[] Events))
				await this.NotificationService.DeleteEvents(Events);

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
				Balance Balance = await this.XmppService.GetEDalerBalance();
				(decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments) = await this.XmppService.GetPendingEDalerPayments();
				(AccountEvent[] Events, bool More) = await this.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);

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
			if (!this.XmppService.TryParseEDalerUri(Uri, out EDalerUri Parsed, out string Reason))
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["InvalidEDalerUri"], Reason));
				return;
			}

			if (Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["ExpiredEDalerUri"]);
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
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnrecognizedEDalerURI"]);
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
				Token Token = await this.XmppService.GetNeuroFeature(TokenId);

				if (!this.NotificationService.TryGetNotificationEvents(EventButton.Wallet, TokenId, out NotificationEvent[] Events))
					Events = new NotificationEvent[0];

				TokenDetailsNavigationArgs Args = new(new TokenItem(Token, this, Events));

				await this.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex);
			}
		}

	}
}
