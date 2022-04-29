using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using EDaler;
using EDaler.Uris;
using IdApp.Pages.Contacts;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using Waher.Networking.XMPP.Contracts;
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
			this.ShowPaymentItemCommand = new Command(async Item => await ShowPaymentItem(Item));
			this.LoadMoreAccountEventsCommand = new Command(async _ => await LoadMoreAccountEvents());
			this.FlipCommand = new Command(async _ => await FlipWallet());
			this.CreateTokenCommand = new Command(async _ => await CreateToken());
			this.LoadMoreTokensCommand = new Command(async _ => await LoadMoreTokens());

			this.PaymentItems = new ObservableCollection<IItemGroup>();
			/* The grouped collection is bugged on Android
			this.PaymentItems = new ObservableCollection<IItemGroupCollection>()
			{
				new ItemGroupCollection<PendingPaymentItem>("PendingPayment", new()),
				new ItemGroupCollection<AccountEventItem>("AccountEvent", new())
			};
			*/

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

			ObservableCollection<IItemGroup> NewPaymentItems = new();

			if (PendingPayments is not null)
			{
				foreach (EDaler.PendingPayment Payment in PendingPayments)
				{
					if (!FriendlyNames.TryGetValue(Payment.To, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Payment.To, this.XmppService.Xmpp, this.TagProfile, this.SmartContracts);
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
						FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote, this.XmppService.Xmpp, this.TagProfile, this.SmartContracts);
						FriendlyNames[Event.Remote] = FriendlyName;
					}

					NewPaymentItems.Add(new AccountEventItem(Event, this, FriendlyName));
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

				if (i >= this.PaymentItems.Count)
					this.PaymentItems.Add(Item);
				else if (this.PaymentItems[i].UniqueName != Item.UniqueName)
					this.PaymentItems.Insert(i, Item);
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
				(EDaler.AccountEvent[] Events, bool More) = await this.XmppService.Wallet.GetAccountEventsAsync(Constants.BatchSizes.AccountEventBatchSize);

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
		/// See <see cref="LastEvent"/>
		/// </summary>
		public static readonly BindableProperty LastEventProperty =
			BindableProperty.Create(nameof(LastEvent), typeof(DateTime), typeof(MyWalletViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime LastEvent
		{
			get => (DateTime)this.GetValue(LastEventProperty);
			set => this.SetValue(LastEventProperty, value);
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
			await this.NavigationService.GoToAsync(nameof(Wallet.RequestPayment.RequestPaymentPage), new EDalerBalanceNavigationArgs(this.Balance));
		}

		private async Task MakePayment()
		{
			await this.NavigationService.GoToAsync(nameof(MyContactsPage),
				new ContactListNavigationArgs(AppResources.SelectContactToPay, SelectContactAction.MakePayment));
		}

		private async Task ShowPaymentItem(object Item)
        {
			if (Item is PendingPaymentItem PendingItem)
			{
				if (!this.XmppService.Wallet.TryParseEDalerUri(PendingItem.Uri, out EDalerUri Uri, out string Reason))
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.InvalidEDalerUri, Reason));
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
				this.HasMoreEvents = false;	// So multiple requests are not made while scrolling.

				try
				{
					EDaler.AccountEvent[] Events;
					bool More;
					int c = this.PaymentItems.Count;

					if (c == 0 || this.PaymentItems[c - 1] is not AccountEventItem LastEvent)
						(Events, More) = await this.XmppService.Wallet.GetAccountEventsAsync(Constants.BatchSizes.AccountEventBatchSize);
					else
						(Events, More) = await this.XmppService.Wallet.GetAccountEventsAsync(Constants.BatchSizes.AccountEventBatchSize, LastEvent.Timestamp);

					this.HasMoreEvents = More;

					if (Events is not null)
					{
						Dictionary<string, string> FriendlyNames = new();

						foreach (EDaler.AccountEvent Event in Events)
						{
							if (!FriendlyNames.TryGetValue(Event.Remote, out string FriendlyName))
							{
								FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote, this.XmppService.Xmpp, this.TagProfile, this.SmartContracts);
								FriendlyNames[Event.Remote] = FriendlyName;
							}

							this.PaymentItems.Add(new AccountEventItem(Event, this, FriendlyName));
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
					TokensEventArgs e = await this.XmppService.Wallet.GetTokens(0, Constants.BatchSizes.TokenBatchSize);

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (e.Ok)
						{
							this.Tokens.Clear();

							if (!(e.Tokens is null))
							{
								foreach (Token Token in e.Tokens)
									this.Tokens.Add(new TokenItem(Token, this));

								this.HasTokens = true;
								this.HasMoreTokens = e.Tokens.Length == Constants.BatchSizes.TokenBatchSize;
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

		private async Task CreateToken()
		{
			try
			{
				// TODO: Let user choose from a list of token templates.

				Contract Template = await this.XmppService.Contracts.GetContract(Constants.ContractTemplates.CreateDemoTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				if (Template.ForMachinesLocalName == "Create" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await this.XmppService.Wallet.GetCreationAttributes();
					XmlDocument Doc = new();
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
								LegalId = e.TrustProviderId,
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
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Template[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Template[CommissionParameter] = e.Commission;
				}

				await this.NavigationService.GoToAsync(nameof(NewContractPage), new NewContractNavigationArgs(Template, true));
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
					TokensEventArgs e = await this.XmppService.Wallet.GetTokens(this.Tokens.Count, Constants.BatchSizes.TokenBatchSize);

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (e.Ok)
						{
							if (!(e.Tokens is null))
							{
								foreach (Token Token in e.Tokens)
									this.Tokens.Add(new TokenItem(Token, this));

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