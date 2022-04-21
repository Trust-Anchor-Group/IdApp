﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using EDaler.Uris;
using IdApp.Pages.Contacts;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MyWallet
{
	/// <summary>
	/// The view model to bind to for when displaying the wallet.
	/// </summary>
	public class MyWalletViewModel : XmppViewModel
	{
		private readonly MyWalletPage page;

		/// <summary>
		/// Creates an instance of the <see cref="MyWalletViewModel"/> class.
		/// </summary>
		public MyWalletViewModel(MyWalletPage Page)
			: base()
		{
			this.page = Page;

			this.BackCommand = new Command(async _ => await GoBack());
			this.ScanQrCodeCommand = new Command(async () => await ScanQrCode());
			this.RequestPaymentCommand = new Command(async _ => await RequestPayment(), _ => this.IsConnected);
			this.MakePaymentCommand = new Command(async _ => await MakePayment(), _ => this.IsConnected);
			this.ShowPendingCommand = new Command(async Item => await ShowPending(Item));
			this.ShowEventCommand = new Command(async Item => await ShowEvent(Item));
			this.FlipCommand = new Command(async _ => await FlipWallet());
			this.LoadMoreTokensCommand = new Command(async _ => await LoadMoreTokens());
			this.TokenSelectedCommand = new Command(async P => await TokenSelected(P));

			this.PendingPayments = new ObservableCollection<PendingPaymentItem>();
			this.Events = new ObservableCollection<AccountEventItem>();
			this.Totals = new ObservableCollection<TokenTotalItem>();
			this.Tokens = new ObservableCollection<TokenItem>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			this.EDalerFrontGlyph = "https://" + this.XmppService.Xmpp.Host + "/Images/eDalerFront200.png";
			this.EDalerBackGlyph = "https://" + this.XmppService.Xmpp.Host + "/Images/eDalerBack200.png";

			if (this.Balance is null && this.NavigationService.TryPopArgs(out WalletNavigationArgs args))
			{
				await AssignProperties(args.Balance, args.PendingAmount, args.PendingCurrency, args.PendingPayments, args.Events,
					args.More, this.XmppService.Wallet.LastEvent);
			}
			else if ((!(this.Balance is null) && !(this.XmppService.Wallet.LastBalance is null) &&
				(this.Balance.Amount != this.XmppService.Wallet.LastBalance.Amount ||
				this.Balance.Currency != this.XmppService.Wallet.LastBalance.Currency ||
				this.Balance.Timestamp != this.XmppService.Wallet.LastBalance.Timestamp)) ||
				this.LastEvent != this.XmppService.Wallet.LastEvent)
			{
				await this.ReloadEDalerWallet(this.XmppService.Wallet.LastBalance ?? Balance ?? this.Balance);
			}

			EvaluateAllCommands();

			this.XmppService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;
			this.XmppService.Wallet.TokenAdded += Wallet_TokenAdded;
			this.XmppService.Wallet.TokenRemoved += Wallet_TokenRemoved;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.XmppService.Wallet.BalanceUpdated -= Wallet_BalanceUpdated;
			this.XmppService.Wallet.TokenAdded -= Wallet_TokenAdded;
			this.XmppService.Wallet.TokenRemoved -= Wallet_TokenRemoved;

			await base.DoUnbind();
		}

		private async Task AssignProperties(Balance Balance, decimal PendingAmount, string PendingCurrency,
			EDaler.PendingPayment[] PendingPayments, EDaler.AccountEvent[] Events, bool More, DateTime LastEvent)
		{
			if (!(Balance is null))
			{
				this.Balance = Balance;
				this.Amount = Balance.Amount;
				this.Currency = Balance.Currency;
				this.Timestamp = Balance.Timestamp;
			}

			this.LastEvent = LastEvent;

			this.PendingAmount = PendingAmount;
			this.PendingCurrency = PendingCurrency;
			this.HasPending = (PendingPayments?.Length ?? 0) > 0;
			this.HasEvents = (Events?.Length ?? 0) > 0;
			this.HasMoreEvents = More;

			Dictionary<string, string> FriendlyNames = new();
			string FriendlyName;

			this.PendingPayments.Clear();
			if (!(PendingPayments is null))
			{
				foreach (EDaler.PendingPayment Payment in PendingPayments)
				{
					if (!FriendlyNames.TryGetValue(Payment.To, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Payment.To, this.XmppService.Xmpp);
						FriendlyNames[Payment.To] = FriendlyName;
					}

					this.PendingPayments.Add(new PendingPaymentItem(Payment, FriendlyName));
				}
			}

			this.Events.Clear();
			if (!(Events is null))
			{
				foreach (EDaler.AccountEvent Event in Events)
				{
					if (!FriendlyNames.TryGetValue(Event.Remote, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote, this.XmppService.Xmpp);
						FriendlyNames[Event.Remote] = FriendlyName;
					}

					this.Events.Add(new AccountEventItem(Event, this, FriendlyName));
				}
			}
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.RequestPaymentCommand, this.MakePaymentCommand);
		}

		private Task Wallet_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			Task.Run(() => this.ReloadEDalerWallet(e.Balance));
			return Task.CompletedTask;
		}

		private async Task ReloadEDalerWallet(Balance Balance)
		{
			try
			{
				(decimal PendingAmount, string PendingCurrency, EDaler.PendingPayment[] PendingPayments) = await this.XmppService.Wallet.GetPendingPayments();
				(EDaler.AccountEvent[] Events, bool More) = await this.XmppService.Wallet.GetAccountEventsAsync(20);

				this.UiSerializer.BeginInvokeOnMainThread(async () => await AssignProperties(Balance, PendingAmount, PendingCurrency,
					PendingPayments, Events, More, this.XmppService.Wallet.LastEvent));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		#region Properties

		/// <summary>
		/// See <see cref="Balance"/>
		/// </summary>
		public static readonly BindableProperty BalanceProperty =
			BindableProperty.Create(nameof(Balance), typeof(Balance), typeof(MyWalletViewModel), default(Balance));

		/// <summary>
		/// Balance of eDaler to process
		/// </summary>
		public Balance Balance
		{
			get { return (Balance)GetValue(BalanceProperty); }
			set { SetValue(BalanceProperty, value); }
		}

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(MyWalletViewModel), default(decimal));

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		public decimal Amount
		{
			get { return (decimal)GetValue(AmountProperty); }
			set { SetValue(AmountProperty, value); }
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create(nameof(Currency), typeof(string), typeof(MyWalletViewModel), default(string));

		/// <summary>
		/// Currency of eDaler to process
		/// </summary>
		public string Currency
		{
			get { return (string)GetValue(CurrencyProperty); }
			set { SetValue(CurrencyProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasPending"/>
		/// </summary>
		public static readonly BindableProperty HasPendingProperty =
			BindableProperty.Create(nameof(HasPending), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasPending of eDaler to process
		/// </summary>
		public bool HasPending
		{
			get => (bool)GetValue(HasPendingProperty);
			set
			{
				SetValue(HasPendingProperty, value);
				this.CalcViewDependencies();
			}
		}

		/// <summary>
		/// See <see cref="IsPendingVisible"/>
		/// </summary>
		public static readonly BindableProperty IsPendingVisibleProperty =
			BindableProperty.Create(nameof(IsPendingVisible), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// IsPendingVisible of eDaler to process
		/// </summary>
		public bool IsPendingVisible
		{
			get { return (bool)GetValue(IsPendingVisibleProperty); }
			set { SetValue(IsPendingVisibleProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsFrontViewShowing"/>
		/// </summary>
		public static readonly BindableProperty IsFrontViewShowingProperty =
			BindableProperty.Create(nameof(IsFrontViewShowing), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// IsFrontViewShowing of eDaler to process
		/// </summary>
		public bool IsFrontViewShowing
		{
			get => (bool)GetValue(IsFrontViewShowingProperty);
			set
			{
				SetValue(IsFrontViewShowingProperty, value);
				this.CalcViewDependencies();
			}
		}

		private void CalcViewDependencies()
		{
			this.IsPendingVisible = this.HasPending && this.IsFrontViewShowing;
			this.AreEventsVisible = this.HasEvents && this.IsFrontViewShowing;
			this.AreTotalsVisible = this.HasTotals && !this.IsFrontViewShowing;
			this.AreTokensVisible = this.HasTokens && !this.IsFrontViewShowing;
		}

		/// <summary>
		/// See <see cref="PendingAmount"/>
		/// </summary>
		public static readonly BindableProperty PendingAmountProperty =
			BindableProperty.Create(nameof(PendingAmount), typeof(decimal), typeof(MyWalletViewModel), default(decimal));

		/// <summary>
		/// PendingAmount of eDaler to process
		/// </summary>
		public decimal PendingAmount
		{
			get { return (decimal)GetValue(PendingAmountProperty); }
			set { SetValue(PendingAmountProperty, value); }
		}

		/// <summary>
		/// See <see cref="PendingCurrency"/>
		/// </summary>
		public static readonly BindableProperty PendingCurrencyProperty =
			BindableProperty.Create(nameof(PendingCurrency), typeof(string), typeof(MyWalletViewModel), default(string));

		/// <summary>
		/// PendingCurrency of eDaler to process
		/// </summary>
		public string PendingCurrency
		{
			get { return (string)GetValue(PendingCurrencyProperty); }
			set { SetValue(PendingCurrencyProperty, value); }
		}

		/// <summary>
		/// See <see cref="Timestamp"/>
		/// </summary>
		public static readonly BindableProperty TimestampProperty =
			BindableProperty.Create(nameof(Timestamp), typeof(DateTime), typeof(MyWalletViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Timestamp
		{
			get { return (DateTime)GetValue(TimestampProperty); }
			set { SetValue(TimestampProperty, value); }
		}

		/// <summary>
		/// See <see cref="LastEvent"/>
		/// </summary>
		public static readonly BindableProperty LastEventProperty =
			BindableProperty.Create(nameof(LastEvent), typeof(DateTime), typeof(MyWalletViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime LastEvent
		{
			get { return (DateTime)GetValue(LastEventProperty); }
			set { SetValue(LastEventProperty, value); }
		}

		/// <summary>
		/// See <see cref="EDalerFrontGlyph"/>
		/// </summary>
		public static readonly BindableProperty EDalerFrontGlyphProperty =
			BindableProperty.Create(nameof(EDalerFrontGlyph), typeof(string), typeof(MyWalletViewModel), default(string));

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		public string EDalerFrontGlyph
		{
			get { return (string)GetValue(EDalerFrontGlyphProperty); }
			set { SetValue(EDalerFrontGlyphProperty, value); }
		}

		/// <summary>
		/// See <see cref="EDalerBackGlyph"/>
		/// </summary>
		public static readonly BindableProperty EDalerBackGlyphProperty =
			BindableProperty.Create(nameof(EDalerBackGlyph), typeof(string), typeof(MyWalletViewModel), default(string));

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		public string EDalerBackGlyph
		{
			get { return (string)GetValue(EDalerBackGlyphProperty); }
			set { SetValue(EDalerBackGlyphProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasEvents"/>
		/// </summary>
		public static readonly BindableProperty HasEventsProperty =
			BindableProperty.Create(nameof(HasEvents), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasEvents of eDaler to process
		/// </summary>
		public bool HasEvents
		{
			get { return (bool)GetValue(HasEventsProperty); }
			set
			{
				SetValue(HasEventsProperty, value);
				this.CalcViewDependencies();
			}
		}

		/// <summary>
		/// See <see cref="AreEventsVisible"/>
		/// </summary>
		public static readonly BindableProperty AreEventsVisibleProperty =
			BindableProperty.Create(nameof(AreEventsVisible), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// AreEventsVisible of eDaler to process
		/// </summary>
		public bool AreEventsVisible
		{
			get { return (bool)GetValue(AreEventsVisibleProperty); }
			set { SetValue(AreEventsVisibleProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasMoreEvents"/>
		/// </summary>
		public static readonly BindableProperty HasMoreEventsProperty =
			BindableProperty.Create(nameof(HasMoreEvents), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// If there are more account events available.
		/// </summary>
		public bool HasMoreEvents
		{
			get { return (bool)GetValue(HasMoreEventsProperty); }
			set { SetValue(HasMoreEventsProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasTotals"/>
		/// </summary>
		public static readonly BindableProperty HasTotalsProperty =
			BindableProperty.Create(nameof(HasTotals), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasTotals of eDaler to process
		/// </summary>
		public bool HasTotals
		{
			get { return (bool)GetValue(HasTotalsProperty); }
			set 
			{
				SetValue(HasTotalsProperty, value);
				this.CalcViewDependencies();
			}
		}

		/// <summary>
		/// See <see cref="AreTotalsVisible"/>
		/// </summary>
		public static readonly BindableProperty AreTotalsVisibleProperty =
			BindableProperty.Create(nameof(AreTotalsVisible), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// AreTotalsVisible of eDaler to process
		/// </summary>
		public bool AreTotalsVisible
		{
			get { return (bool)GetValue(AreTotalsVisibleProperty); }
			set { SetValue(AreTotalsVisibleProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasTokens"/>
		/// </summary>
		public static readonly BindableProperty HasTokensProperty =
			BindableProperty.Create(nameof(HasTokens), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasTokens of eDaler to process
		/// </summary>
		public bool HasTokens
		{
			get { return (bool)GetValue(HasTokensProperty); }
			set 
			{ 
				SetValue(HasTokensProperty, value);
				this.CalcViewDependencies();
			}
		}

		/// <summary>
		/// See <see cref="AreTokensVisible"/>
		/// </summary>
		public static readonly BindableProperty AreTokensVisibleProperty =
			BindableProperty.Create(nameof(AreTokensVisible), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// AreTokensVisible of eDaler to process
		/// </summary>
		public bool AreTokensVisible
		{
			get { return (bool)GetValue(AreTokensVisibleProperty); }
			set { SetValue(AreTokensVisibleProperty, value); }
		}

		/// <summary>
		/// Holds a list of pending payments
		/// </summary>
		public ObservableCollection<PendingPaymentItem> PendingPayments { get; }

		/// <summary>
		/// Holds a list of account events
		/// </summary>
		public ObservableCollection<AccountEventItem> Events { get; }

		/// <summary>
		/// Holds a list of token totals
		/// </summary>
		public ObservableCollection<TokenTotalItem> Totals { get; }

		/// <summary>
		/// Holds a list of tokens
		/// </summary>
		public ObservableCollection<TokenItem> Tokens { get; }

		/// <summary>
		/// The command to bind to for returning to previous view.
		/// </summary>
		public ICommand BackCommand { get; }

		/// <summary>
		/// The command to bind to for allowing users to scan QR codes.
		/// </summary>
		public ICommand ScanQrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for requesting a payment
		/// </summary>
		public ICommand RequestPaymentCommand { get; }

		/// <summary>
		/// The command to bind to for making a payment
		/// </summary>
		public ICommand MakePaymentCommand { get; }

		/// <summary>
		/// The command to bind to for displaying information about a pending payment.
		/// </summary>
		public ICommand ShowPendingCommand { get; }

		/// <summary>
		/// The command to bind to for displaying information about an account event.
		/// </summary>
		public ICommand ShowEventCommand { get; }

		/// <summary>
		/// The command to bind to for flipping the wallet.
		/// </summary>
		public ICommand FlipCommand { get; }

		/// <summary>
		/// Command executed when more tokens need to be loaded.
		/// </summary>
		public ICommand LoadMoreTokensCommand { get; }

		/// <summary>
		/// Command executed when a token has been selected.
		/// </summary>
		public ICommand TokenSelectedCommand { get; }

		#endregion

		private async Task GoBack()
		{
			await this.NavigationService.GoBackAsync();
		}

		private async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult();
		}

		private async Task RequestPayment()
		{
			await this.NavigationService.GoToAsync(nameof(Wallet.RequestPayment.RequestPaymentPage), new EDalerBalanceNavigationArgs(this.Balance));
		}

		private async Task MakePayment()
		{
			await this.NavigationService.GoToAsync(nameof(MyContactsPage),
				new ContactListNavigationArgs(AppResources.SelectContactToPay, SelectContactAction.MakePayment));
		}

		private async Task ShowPending(object P)
		{
			if (P is not PendingPaymentItem Item)
				return;

			if (!this.XmppService.Wallet.TryParseEDalerUri(Item.Uri, out EDalerUri Uri, out string Reason))
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.InvalidEDalerUri, Reason));
				return;
			}

			await this.NavigationService.GoToAsync(nameof(PendingPayment.PendingPaymentPage), new EDalerUriNavigationArgs(Uri, Item.FriendlyName));
		}

		private async Task ShowEvent(object P)
		{
			if (P is not AccountEventItem Item)
				return;

			await this.NavigationService.GoToAsync(nameof(AccountEvent.AccountEventPage), new AccountEvent.AccountEventNavigationArgs(Item));
		}

		private Task FlipWallet()
		{
			this.page.WalletFlipView_Tapped(this, EventArgs.Empty);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Binds token information to the view.
		/// </summary>
		public async void BindTokens()
		{
			if (!this.HasTotals)
			{
				try
				{
					TokenTotalsEventArgs e = await this.XmppService.Wallet.GetTotals();

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (e.Ok)
						{
							this.Totals.Clear();

							if (!(e.Totals is null))
							{
								foreach (TokenTotal Total in e.Totals)
									this.Totals.Add(new TokenTotalItem(Total));

								this.HasTotals = true;
							}
							else
								this.HasTotals = false;
						}
						else
							this.HasTotals = false;
					});
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}

			if (!this.HasTokens)
			{
				try
				{
					TokensEventArgs e = await this.XmppService.Wallet.GetTokens(0, 20);

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (e.Ok)
						{
							this.Tokens.Clear();

							if (!(e.Tokens is null))
							{
								foreach (Token Token in e.Tokens)
									this.Tokens.Add(new TokenItem(Token));

								this.HasTokens = true;
							}
							else
								this.HasTokens = false;
						}
						else
							this.HasTokens = false;
					});
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		internal void ViewsFlipped(bool IsFrontViewShowing)
		{
			this.IsFrontViewShowing = IsFrontViewShowing;
		}

		private Task LoadMoreTokens()
		{
			return Task.CompletedTask;  // TODO
		}

		private Task TokenSelected(object P)
		{
			return Task.CompletedTask;  // TODO
		}

		private Task Wallet_TokenRemoved(object Sender, TokenEventArgs e)
		{
			return Task.CompletedTask;  // TODO
		}

		private Task Wallet_TokenAdded(object Sender, TokenEventArgs e)
		{
			return Task.CompletedTask;  // TODO
		}

	}
}