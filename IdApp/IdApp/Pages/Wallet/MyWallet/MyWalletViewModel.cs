using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using EDaler;
using EDaler.Uris;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts;
using IdApp.Pages.Contracts.MyContracts;
using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Registration.ValidateIdentity;
using IdApp.Pages.Wallet.BuyEDaler;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using IdApp.Pages.Wallet.RequestPayment;
using IdApp.Pages.Wallet.SellEDaler;
using IdApp.Pages.Wallet.ServiceProviders;
using IdApp.Services;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using IdApp.Services.Notification.Wallet;
using IdApp.Services.Wallet;
using NeuroFeatures;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
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

			this.BackCommand = new Command(async _ => await this.GoBack());
			this.ScanQrCodeCommand = new Command(async () => await this.ScanQrCode());
			this.RequestPaymentCommand = new Command(async _ => await this.RequestPayment(), _ => this.IsConnected);
			this.MakePaymentCommand = new Command(async _ => await this.MakePayment(), _ => this.IsConnected);
			this.ShowPaymentItemCommand = new Command(async Item => await this.ShowPaymentItem(Item));
			this.LoadMoreAccountEventsCommand = new Command(async _ => await this.LoadMoreAccountEvents());
			this.FlipCommand = new Command(async _ => await this.FlipWallet());
			this.CreateTokenCommand = new Command(async _ => await this.CreateToken());
			this.LoadMoreTokensCommand = new Command(async _ => await this.LoadMoreTokens());

			this.PaymentItems = new ObservableCollection<IItemGroup>();
			/* The grouped collection is bugged on Android
			this.PaymentItems = new ObservableCollection<IItemGroupCollection>()
			{
				new ItemGroupCollection<PendingPaymentItem>("PendingPayment", new()),
				new ItemGroupCollection<AccountEventItem>("AccountEvent", new())
			};
			*/

			this.Totals = new ObservableCollection<TokenTotalItem>();
			this.Tokens = new ObservableCollection<object>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.EDalerFrontGlyph = "https://" + this.TagProfile.Domain + "/Images/eDalerFront200.png";
			this.EDalerBackGlyph = "https://" + this.TagProfile.Domain + "/Images/eDalerBack200.png";

			if (this.Balance is null && this.NavigationService.TryGetArgs(out WalletNavigationArgs args))
			{
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents = this.GetNotificationEvents();

				await this.AssignProperties(args.Balance, args.PendingAmount, args.PendingCurrency, args.PendingPayments,
					args.Events, args.More, this.XmppService.LastEDalerEvent, NotificationEvents);
			}
			else if (((this.Balance is not null) && (this.XmppService.LastEDalerBalance is not null) &&
				(this.Balance.Amount != this.XmppService.LastEDalerBalance.Amount ||
				this.Balance.Currency != this.XmppService.LastEDalerBalance.Currency ||
				this.Balance.Timestamp != this.XmppService.LastEDalerBalance.Timestamp)) ||
				this.LastEDalerEvent != this.XmppService.LastEDalerEvent)
			{
				await this.ReloadEDalerWallet(this.XmppService.LastEDalerBalance ?? this.Balance ?? this.Balance);
			}

			if (this.HasTokens && this.LastTokenEvent != this.XmppService.LastNeuroFeatureEvent)
				await this.LoadTokens(true);

			this.EvaluateAllCommands();

			this.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
			this.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			this.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			this.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
			this.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			this.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
			this.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;

			await base.OnDispose();
		}

		private SortedDictionary<CaseInsensitiveString, NotificationEvent[]> GetNotificationEvents()
		{
			SortedDictionary<CaseInsensitiveString, NotificationEvent[]> Result = this.NotificationService.GetEventsByCategory(EventButton.Wallet);
			int NrBalance = 0;
			int NrToken = 0;

			foreach (NotificationEvent[] Events in Result.Values)
			{
				foreach (NotificationEvent Event in Events)
				{
					if (Event is BalanceNotificationEvent)
						NrBalance++;
					else if (Event is TokenNotificationEvent)
						NrToken++;
				}
			}

			this.NrBalanceNotifications = NrBalance;
			this.NrTokenNotifications = NrToken;

			return Result;
		}

		private async Task AssignProperties(Balance Balance, decimal PendingAmount, string PendingCurrency,
			EDaler.PendingPayment[] PendingPayments, EDaler.AccountEvent[] Events, bool More, DateTime LastEvent,
			SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents)
		{
			if (Balance is not null)
			{
				this.Balance = Balance;
				this.Amount = Balance.Amount;
				this.ReservedAmount = Balance.Reserved;
				this.Currency = Balance.Currency;
				this.Timestamp = Balance.Timestamp;
			}

			this.LastEDalerEvent = LastEvent;

			this.PendingAmount = PendingAmount;
			this.PendingCurrency = PendingCurrency;
			this.HasPending = (PendingPayments?.Length ?? 0) > 0;
			this.HasEvents = (Events?.Length ?? 0) > 0;
			this.HasMoreEvents = More;

			Dictionary<string, string> FriendlyNames = new();
			string FriendlyName;

			ObservableCollection<IItemGroup> NewPaymentItems = new();

			if (PendingPayments is not null)
			{
				foreach (EDaler.PendingPayment Payment in PendingPayments)
				{
					if (!FriendlyNames.TryGetValue(Payment.To, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Payment.To, this);
						FriendlyNames[Payment.To] = FriendlyName;
					}

					NewPaymentItems.Add(new PendingPaymentItem(Payment, FriendlyName));
				}
			}

			if (Events is not null)
			{
				foreach (EDaler.AccountEvent Event in Events)
				{
					if (!FriendlyNames.TryGetValue(Event.Remote, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote, this);
						FriendlyNames[Event.Remote] = FriendlyName;
					}

					if (!NotificationEvents.TryGetValue(Event.TransactionId.ToString(), out NotificationEvent[] CategoryEvents))
						CategoryEvents = new NotificationEvent[0];

					NewPaymentItems.Add(new AccountEventItem(Event, this, FriendlyName, CategoryEvents, this));
				}
			}

			this.MergeObservableCollections(this.PaymentItems, NewPaymentItems);
		}

		private void MergeObservableCollections(ObservableCollection<IItemGroup> OldCollection, ObservableCollection<IItemGroup> NewCollection)
		{
			List<IItemGroup> RemoveItems = OldCollection.Where(oel => NewCollection.All(nel => nel.UniqueName != oel.UniqueName)).ToList();

			foreach (IItemGroup Item in RemoveItems)
				OldCollection.Remove(Item);

			for (int i = 0; i < NewCollection.Count; i++)
			{
				IItemGroup Item = NewCollection[i];

				if (i >= OldCollection.Count)
					OldCollection.Add(Item);
				else if (OldCollection[i].UniqueName != Item.UniqueName)
					OldCollection.Insert(i, Item);
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
				(decimal PendingAmount, string PendingCurrency, EDaler.PendingPayment[] PendingPayments) = await this.XmppService.GetPendingEDalerPayments();
				(EDaler.AccountEvent[] Events, bool More) = await this.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents = this.GetNotificationEvents();

				this.UiSerializer.BeginInvokeOnMainThread(async () => await this.AssignProperties(Balance, PendingAmount, PendingCurrency,
					PendingPayments, Events, More, this.XmppService.LastEDalerEvent, NotificationEvents));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object Sender, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
			});

			return Task.CompletedTask;
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
			get => (Balance)this.GetValue(BalanceProperty);
			set => this.SetValue(BalanceProperty, value);
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
			get => (decimal)this.GetValue(AmountProperty);
			set => this.SetValue(AmountProperty, value);
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
			get => (string)this.GetValue(CurrencyProperty);
			set => this.SetValue(CurrencyProperty, value);
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
			get => (bool)this.GetValue(HasPendingProperty);
			set => this.SetValue(HasPendingProperty, value);
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
			get => (bool)this.GetValue(IsFrontViewShowingProperty);
			set => this.SetValue(IsFrontViewShowingProperty, value);
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
			get => (decimal)this.GetValue(PendingAmountProperty);
			set => this.SetValue(PendingAmountProperty, value);
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
			get => (string)this.GetValue(PendingCurrencyProperty);
			set => this.SetValue(PendingCurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="ReservedAmount"/>
		/// </summary>
		public static readonly BindableProperty ReservedAmountProperty =
			BindableProperty.Create(nameof(ReservedAmount), typeof(decimal), typeof(MyWalletViewModel), default(decimal));

		/// <summary>
		/// ReservedAmount of eDaler to process
		/// </summary>
		public decimal ReservedAmount
		{
			get => (decimal)this.GetValue(ReservedAmountProperty);
			set => this.SetValue(ReservedAmountProperty, value);
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
			get => (DateTime)this.GetValue(TimestampProperty);
			set => this.SetValue(TimestampProperty, value);
		}

		/// <summary>
		/// See <see cref="LastEDalerEvent"/>
		/// </summary>
		public static readonly BindableProperty LastEDalerEventProperty =
			BindableProperty.Create(nameof(LastEDalerEvent), typeof(DateTime), typeof(MyWalletViewModel), default(DateTime));

		/// <summary>
		/// When last eDaler event was received.
		/// </summary>
		public DateTime LastEDalerEvent
		{
			get => (DateTime)this.GetValue(LastEDalerEventProperty);
			set => this.SetValue(LastEDalerEventProperty, value);
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
			get => (string)this.GetValue(EDalerFrontGlyphProperty);
			set => this.SetValue(EDalerFrontGlyphProperty, value);
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
			get => (string)this.GetValue(EDalerBackGlyphProperty);
			set => this.SetValue(EDalerBackGlyphProperty, value);
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
			get => (bool)this.GetValue(HasEventsProperty);
			set => this.SetValue(HasEventsProperty, value);
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
			get => (bool)this.GetValue(HasMoreEventsProperty);
			set => this.SetValue(HasMoreEventsProperty, value);
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
			get => (bool)this.GetValue(HasTotalsProperty);
			set => this.SetValue(HasTotalsProperty, value);
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
			get => (bool)this.GetValue(HasTokensProperty);
			set => this.SetValue(HasTokensProperty, value);
		}

		/// <summary>
		/// See <see cref="HasMoreTokens"/>
		/// </summary>
		public static readonly BindableProperty HasMoreTokensProperty =
			BindableProperty.Create(nameof(HasMoreTokens), typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasMoreTokens of eDaler to process
		/// </summary>
		public bool HasMoreTokens
		{
			get => (bool)this.GetValue(HasMoreTokensProperty);
			set => this.SetValue(HasMoreTokensProperty, value);
		}

		/// <summary>
		/// See <see cref="LastTokenEvent"/>
		/// </summary>
		public static readonly BindableProperty LastTokenEventProperty =
			BindableProperty.Create(nameof(LastTokenEvent), typeof(DateTime), typeof(MyWalletViewModel), default(DateTime));

		/// <summary>
		/// When last eDaler event was received.
		/// </summary>
		public DateTime LastTokenEvent
		{
			get => (DateTime)this.GetValue(LastTokenEventProperty);
			set => this.SetValue(LastTokenEventProperty, value);
		}

		/// <summary>
		/// See <see cref="NrBalanceNotifications"/>
		/// </summary>
		public static readonly BindableProperty NrBalanceNotificationsProperty =
			BindableProperty.Create(nameof(NrBalanceNotifications), typeof(int), typeof(MyWalletViewModel), default(int));

		/// <summary>
		/// When last eDaler event was received.
		/// </summary>
		public int NrBalanceNotifications
		{
			get => (int)this.GetValue(NrBalanceNotificationsProperty);
			set => this.SetValue(NrBalanceNotificationsProperty, value);
		}

		/// <summary>
		/// See <see cref="NrTokenNotifications"/>
		/// </summary>
		public static readonly BindableProperty NrTokenNotificationsProperty =
			BindableProperty.Create(nameof(NrTokenNotifications), typeof(int), typeof(MyWalletViewModel), default(int));

		/// <summary>
		/// When last eDaler event was received.
		/// </summary>
		public int NrTokenNotifications
		{
			get => (int)this.GetValue(NrTokenNotificationsProperty);
			set => this.SetValue(NrTokenNotificationsProperty, value);
		}

		/// <summary>
		/// Holds a list of pending payments and account events
		/// </summary>
		//public ObservableCollection<IItemGroupCollection> PaymentItems { get; }
		public ObservableCollection<IItemGroup> PaymentItems { get; }

		/// <summary>
		/// Holds a list of token totals
		/// </summary>
		public ObservableCollection<TokenTotalItem> Totals { get; }

		/// <summary>
		/// Holds a list of tokens
		/// </summary>
		public ObservableCollection<object> Tokens { get; }

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
		/// The command to bind to for displaying information about a pending payment or an account event.
		/// </summary>
		public ICommand ShowPaymentItemCommand { get; }

		/// <summary>
		/// Command executed when more account events need to be displayed.
		/// </summary>
		public ICommand LoadMoreAccountEventsCommand { get; }

		/// <summary>
		/// The command to bind to for flipping the wallet.
		/// </summary>
		public ICommand FlipCommand { get; }

		/// <summary>
		/// The command to bind to for creating tokens
		/// </summary>
		public ICommand CreateTokenCommand { get; }

		/// <summary>
		/// Command executed when more tokens need to be loaded.
		/// </summary>
		public ICommand LoadMoreTokensCommand { get; }

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
			try
			{
				IBuyEDalerServiceProvider[] ServiceProviders = await this.XmppService.GetServiceProvidersForBuyingEDalerAsync();

				if (ServiceProviders.Length == 0)
				{
					EDalerBalanceNavigationArgs Args = new(this.Balance);

					await this.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.ToThisPage);
				}
				else
				{
					List<IBuyEDalerServiceProvider> ServiceProviders2 = new();

					ServiceProviders2.AddRange(ServiceProviders);
					ServiceProviders2.Add(new EmptyBuyEDalerServiceProvider());

					ServiceProvidersNavigationArgs e = new(ServiceProviders2.ToArray(),
						LocalizationResourceManager.Current["BuyEDaler"],
						LocalizationResourceManager.Current["SelectServiceProviderBuyEDaler"]);

					await this.NavigationService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);

					IBuyEDalerServiceProvider ServiceProvider = (IBuyEDalerServiceProvider)await e.ServiceProvider.Task;

					if (ServiceProvider is not null)
					{
						if (string.IsNullOrEmpty(ServiceProvider.Id))
						{
							EDalerBalanceNavigationArgs Args = new(this.Balance);

							await this.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.ToThisPage);
						}
						else if (string.IsNullOrEmpty(ServiceProvider.BuyEDalerTemplateContractId))
						{
							TaskCompletionSource<decimal?> Result = new();
							BuyEDalerNavigationArgs Args = new(this.Balance.Currency, Result);

							await this.NavigationService.GoToAsync(nameof(BuyEDalerPage), Args, BackMethod.ToThisPage);

							decimal? Amount = await Result.Task;

							if (Amount.HasValue && Amount.Value > 0)
							{
								PaymentTransaction Transaction = await this.XmppService.InitiateBuyEDaler(ServiceProvider.Id, ServiceProvider.Type,
									Amount.Value, this.Balance.Currency);

								this.WaitForComletion(Transaction);
							}
						}
						else
						{
							CreationAttributesEventArgs e2 = await this.XmppService.GetNeuroFeatureCreationAttributes();
							Dictionary<CaseInsensitiveString, object> Parameters = new()
							{
								{ "Visibility", "CreatorAndParts" },
								{ "Role", "Buyer" },
								{ "Currency", this.Balance?.Currency ?? e2.Currency },
								{ "TrustProvider", e2.TrustProviderId }
							};

							await this.ContractOrchestratorService.OpenContract(ServiceProvider.BuyEDalerTemplateContractId,
								LocalizationResourceManager.Current["BuyEDaler"], Parameters);

							OptionsTransaction OptionsTransaction = await this.XmppService.InitiateBuyEDalerGetOptions(ServiceProvider.Id, ServiceProvider.Type);
							IDictionary<CaseInsensitiveString, object>[] Options = await OptionsTransaction.Wait();

							if (this.NavigationService.CurrentPage is IContractOptionsPage ContractOptionsPage)
								this.UiSerializer.BeginInvokeOnMainThread(() => ContractOptionsPage.ShowContractOptions(Options));
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void WaitForComletion(PaymentTransaction Transaction)
		{
			try
			{
				await Transaction.Wait();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task MakePayment()
		{
			try
			{
				ISellEDalerServiceProvider[] ServiceProviders = await this.XmppService.GetServiceProvidersForSellingEDalerAsync();

				if (ServiceProviders.Length == 0)
				{
					ContactListNavigationArgs Args = new(LocalizationResourceManager.Current["SelectContactToPay"], SelectContactAction.MakePayment)
					{
						CanScanQrCode = true,
						AllowAnonymous = true,
						AnonymousText = LocalizationResourceManager.Current["Open"]
					};

					await this.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.ToThisPage);
				}
				else
				{
					List<ISellEDalerServiceProvider> ServiceProviders2 = new();

					ServiceProviders2.AddRange(ServiceProviders);
					ServiceProviders2.Add(new EmptySellEDalerServiceProvider());

					ServiceProvidersNavigationArgs e = new(ServiceProviders2.ToArray(),
						LocalizationResourceManager.Current["SellEDaler"],
						LocalizationResourceManager.Current["SelectServiceProviderSellEDaler"]);

					await this.NavigationService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);

					ISellEDalerServiceProvider ServiceProvider = (ISellEDalerServiceProvider)await e.ServiceProvider.Task;

					if (ServiceProvider is not null)
					{
						if (string.IsNullOrEmpty(ServiceProvider.Id))
						{
							ContactListNavigationArgs Args = new(LocalizationResourceManager.Current["SelectContactToPay"], SelectContactAction.MakePayment)
							{
								CanScanQrCode = true,
								AllowAnonymous = true,
								AnonymousText = LocalizationResourceManager.Current["Open"],
							};

							await this.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.ToThisPage);
						}
						else if (string.IsNullOrEmpty(ServiceProvider.SellEDalerTemplateContractId))
						{
							TaskCompletionSource<decimal?> Result = new();
							SellEDalerNavigationArgs Args = new(this.Balance.Currency, Result);

							await this.NavigationService.GoToAsync(nameof(SellEDalerPage), Args, BackMethod.ToThisPage);

							decimal? Amount = await Result.Task;

							if (Amount.HasValue && Amount.Value > 0)
							{
								PaymentTransaction Transaction = await this.XmppService.InitiateSellEDaler(ServiceProvider.Id, ServiceProvider.Type,
									Amount.Value, this.Balance.Currency);

								this.WaitForComletion(Transaction);
							}
						}
						else
						{
							CreationAttributesEventArgs e2 = await this.XmppService.GetNeuroFeatureCreationAttributes();
							Dictionary<CaseInsensitiveString, object> Parameters = new()
							{
								{ "Visibility", "CreatorAndParts" },
								{ "Role", "Seller" },
								{ "Currency", this.Balance?.Currency ?? e2.Currency },
								{ "TrustProvider", e2.TrustProviderId }
							};

							await this.ContractOrchestratorService.OpenContract(ServiceProvider.SellEDalerTemplateContractId,
								LocalizationResourceManager.Current["SellEDaler"], Parameters);

							OptionsTransaction OptionsTransaction = await this.XmppService.InitiateSellEDalerGetOptions(ServiceProvider.Id, ServiceProvider.Type);
							IDictionary<CaseInsensitiveString, object>[] Options = await OptionsTransaction.Wait();

							if (this.NavigationService.CurrentPage is IContractOptionsPage ContractOptionsPage)
								this.UiSerializer.BeginInvokeOnMainThread(() => ContractOptionsPage.ShowContractOptions(Options));
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ShowPaymentItem(object Item)
		{
			if (Item is PendingPaymentItem PendingItem)
			{
				if (!this.XmppService.TryParseEDalerUri(PendingItem.Uri, out EDalerUri Uri, out string Reason))
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["InvalidEDalerUri"], Reason));
					return;
				}

				await this.NavigationService.GoToAsync(nameof(PendingPayment.PendingPaymentPage), new EDalerUriNavigationArgs(Uri, PendingItem.FriendlyName));
			}
			else if (Item is AccountEventItem EventItem)
			{
				await this.NavigationService.GoToAsync(nameof(AccountEvent.AccountEventPage), new AccountEvent.AccountEventNavigationArgs(EventItem));
			}
		}

		private async Task LoadMoreAccountEvents()
		{
			if (this.HasMoreEvents)
			{
				this.HasMoreEvents = false; // So multiple requests are not made while scrolling.

				try
				{
					EDaler.AccountEvent[] Events;
					bool More;
					int c = this.PaymentItems.Count;

					if (c == 0 || this.PaymentItems[c - 1] is not AccountEventItem LastEvent)
						(Events, More) = await this.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);
					else
						(Events, More) = await this.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize, LastEvent.Timestamp);

					this.HasMoreEvents = More;

					if (Events is not null)
					{
						Dictionary<string, string> FriendlyNames = new();
						SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents = this.GetNotificationEvents();

						foreach (EDaler.AccountEvent Event in Events)
						{
							if (!FriendlyNames.TryGetValue(Event.Remote, out string FriendlyName))
							{
								FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote, this);
								FriendlyNames[Event.Remote] = FriendlyName;
							}

							if (!NotificationEvents.TryGetValue(Event.TransactionId.ToString(), out NotificationEvent[] CategoryEvents))
								CategoryEvents = new NotificationEvent[0];

							this.PaymentItems.Add(new AccountEventItem(Event, this, FriendlyName, CategoryEvents, this));
						}
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
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
			try
			{
				await this.LoadTokens(false);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async Task LoadTokens(bool Reload)
		{
			this.LastTokenEvent = this.XmppService.LastNeuroFeatureEvent;

			if (!this.HasTotals || Reload)
			{
				try
				{
					TokenTotalsEventArgs e = await this.XmppService.GetNeuroFeatureTotals();

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						this.Totals.Clear();

						if (e.Ok && e.Totals is not null)
						{
							this.Totals.Clear();

							foreach (TokenTotal Total in e.Totals)
								this.Totals.Add(new TokenTotalItem(Total));

							this.HasTotals = true;
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

			if (!this.HasTokens || Reload)
			{
				try
				{
					SortedDictionary<CaseInsensitiveString, TokenNotificationEvent[]> NotificationEvents =
						this.NotificationService.GetEventsByCategory<TokenNotificationEvent>(EventButton.Wallet);

					TokensEventArgs e = await this.XmppService.GetNeuroFeatures(0, Constants.BatchSizes.TokenBatchSize);
					SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCateogy = this.GetNotificationEvents();

					this.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						try
						{
							this.Tokens.Clear();

							foreach (KeyValuePair<CaseInsensitiveString, TokenNotificationEvent[]> P in NotificationEvents)
							{
								Token Token = null;

								foreach (TokenNotificationEvent TokenEvent in P.Value)
								{
									Token = TokenEvent.Token;
									if (Token is not null)
										break;
								}

								if (Token is not null)
									this.Tokens.Add(new TokenItem(Token, this, P.Value));
								else
								{
									List<TokenNotificationEvent> ToDelete = null;

									foreach (TokenNotificationEvent TokenEvent in P.Value)
									{
										if (TokenEvent is TokenRemovedNotificationEvent)
										{
											string Icon = await TokenEvent.GetCategoryIcon(this);
											string Description = await TokenEvent.GetDescription(this);

											this.Tokens.Add(new EventModel(TokenEvent.Received, Icon, Description, TokenEvent, this));
										}
										else
										{
											ToDelete ??= new List<TokenNotificationEvent>();
											ToDelete.Add(TokenEvent);
										}
									}

									if (ToDelete is not null)
										await this.NotificationService.DeleteEvents(ToDelete.ToArray());
								}
							}

							if (e.Ok && e.Tokens is not null)
							{
								foreach (Token Token in e.Tokens)
								{
									if (NotificationEvents.ContainsKey(Token.TokenId))
										continue;

									if (!EventsByCateogy.TryGetValue(Token.TokenId, out NotificationEvent[] Events))
										Events = new NotificationEvent[0];

									this.Tokens.Add(new TokenItem(Token, this, Events));
								}

								this.HasTokens = true;
								this.HasMoreTokens = e.Tokens.Length == Constants.BatchSizes.TokenBatchSize;
							}
							else
								this.HasTokens = false;
						}
						catch (Exception ex)
						{
							this.LogService.LogException(ex);
						}
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

		private async Task CreateToken()
		{
			try
			{
				TaskCompletionSource<Contract> TemplateSelection = new();
				MyContractsNavigationArgs Args = new(ContractsListMode.TokenCreationTemplates, TemplateSelection);

				await this.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);

				Contract Template = await TemplateSelection.Task;
				if (Template is null)
					return;

				Dictionary<CaseInsensitiveString, object> Parameters = new();
				Template.Visibility = ContractVisibility.Public;

				if (Template.ForMachinesLocalName == "Create" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e2 = await this.XmppService.GetNeuroFeatureCreationAttributes();
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Template.ForMachines.OuterXml);

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string CreatorRole = Doc.SelectSingleNode("/nft:Create/nft:Creator/nft:RoleReference/@role", NamespaceManager)?.Value;
					string OwnerRole = Doc.SelectSingleNode("/nft:Create/nft:Owner/nft:RoleReference/@role", NamespaceManager)?.Value;
					string TrustProviderRole = Doc.SelectSingleNode("/nft:Create/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string CurrencyParameter = Doc.SelectSingleNode("/nft:Create/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string CommissionParameter = Doc.SelectSingleNode("/nft:Create/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = new();

						if (!string.IsNullOrEmpty(CreatorRole))
						{
							Parts.Add(new Part()
							{
								LegalId = this.TagProfile.LegalIdentity.Id,
								Role = CreatorRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e2.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = Parts.ToArray();
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == CreatorRole || Part.Role == OwnerRole)
								Part.LegalId = this.TagProfile.LegalIdentity.Id;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e2.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Parameters[CurrencyParameter] = e2.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Parameters[CommissionParameter] = e2.Commission;
				}

				NewContractNavigationArgs NewContractArgs = new(Template, true, Parameters);

				await this.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.ToThisPage);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task LoadMoreTokens()
		{
			if (this.HasMoreTokens)
			{
				this.HasMoreTokens = false; // So multiple requests are not made while scrolling.

				try
				{
					TokensEventArgs e = await this.XmppService.GetNeuroFeatures(this.Tokens.Count, Constants.BatchSizes.TokenBatchSize);
					SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCateogy = this.GetNotificationEvents();

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (e.Ok)
						{
							if (e.Tokens is not null)
							{
								foreach (Token Token in e.Tokens)
								{
									if (!EventsByCateogy.TryGetValue(Token.TokenId, out NotificationEvent[] Events))
										Events = new NotificationEvent[0];

									this.Tokens.Add(new TokenItem(Token, this, Events));
								}

								this.HasMoreTokens = e.Tokens.Length == Constants.BatchSizes.TokenBatchSize;
							}
						}
					});
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		private Task Wallet_TokenAdded(object _, TokenEventArgs e)
		{
			if (!this.NotificationService.TryGetNotificationEvents(EventButton.Wallet, e.Token.TokenId, out NotificationEvent[] Events))
				Events = new NotificationEvent[0];

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				TokenItem Item = new(e.Token, this, Events);

				if (this.Tokens.Count == 0)
					this.Tokens.Add(Item);
				else
					this.Tokens.Insert(0, Item);
			});

			return Task.CompletedTask;
		}

		private Task Wallet_TokenRemoved(object _, TokenEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				int i, c = this.Tokens.Count;

				for (i = 0; i < c; i++)
				{
					if (this.Tokens[i] is TokenItem Item && Item.TokenId == e.Token.TokenId)
					{
						this.Tokens.RemoveAt(i);
						break;
					}
				}
			});

			return Task.CompletedTask;
		}

		private void NotificationService_OnNewNotification(object Sender, NotificationEventArgs e)
		{
			if (e.Event.Button == EventButton.Wallet)
			{
				this.UiSerializer.BeginInvokeOnMainThread(() =>
				{
					if (e.Event is BalanceNotificationEvent)
						this.NrBalanceNotifications++;
					else if (e.Event is TokenNotificationEvent)
						this.NrTokenNotifications++;
				});
			}
		}

	}
}
