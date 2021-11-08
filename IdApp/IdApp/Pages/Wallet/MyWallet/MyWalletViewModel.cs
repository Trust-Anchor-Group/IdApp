using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using EDaler.Uris;
using IdApp.Pages.Contacts;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Services;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.ThingRegistries;
using IdApp.Services.UI;
using IdApp.Services.Wallet;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MyWallet
{
	/// <summary>
	/// The view model to bind to for when displaying the wallet.
	/// </summary>
	public class MyWalletViewModel : NeuronViewModel
	{
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INetworkService networkService;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly IThingRegistryOrchestratorService thingRegistryOrchestratorService;
		private readonly IEDalerOrchestratorService eDalerOrchestratorService;

		/// <summary>
		/// Creates an instance of the <see cref="MyWalletViewModel"/> class.
		/// </summary>
		public MyWalletViewModel(
			ITagProfile tagProfile,
			IUiSerializer uiSerializer,
			INeuronService neuronService,
			INavigationService navigationService,
			INetworkService networkService,
			ILogService logService,
			IContractOrchestratorService contractOrchestratorService,
			IThingRegistryOrchestratorService thingThingRegistryOrchestratorService,
			IEDalerOrchestratorService eDalerOrchestratorService)
		: base(neuronService, uiSerializer, tagProfile)
		{
			this.logService = logService;
			this.navigationService = navigationService;
			this.networkService = networkService;
			this.contractOrchestratorService = contractOrchestratorService ?? App.Instantiate<IContractOrchestratorService>();
			this.thingRegistryOrchestratorService = thingThingRegistryOrchestratorService ?? App.Instantiate<IThingRegistryOrchestratorService>();
			this.eDalerOrchestratorService = eDalerOrchestratorService ?? App.Instantiate<IEDalerOrchestratorService>();

			this.BackCommand = new Command(async _ => await GoBack());
			this.ScanQrCodeCommand = new Command(async () => await ScanQrCode());
			this.RequestPaymentCommand = new Command(async _ => await RequestPayment(), _ => IsConnected);
			this.MakePaymentCommand = new Command(async _ => await MakePayment(), _ => IsConnected);
			this.ShowPendingCommand = new Command(async Item => await ShowPending(Item));
			this.ShowEventCommand = new Command(async Item => await ShowEvent(Item));

			this.PendingPayments = new ObservableCollection<PendingPaymentItem>();
			this.Events = new ObservableCollection<AccountEventItem>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.Balance is null && this.navigationService.TryPopArgs(out WalletNavigationArgs args))
			{
				await AssignProperties(args.Balance, args.PendingAmount, args.PendingCurrency, args.PendingPayments, args.Events, args.More);

				StringBuilder Url = new StringBuilder();

				Url.Append("https://");
				Url.Append(this.NeuronService.Xmpp.Host);
				Url.Append("/Images/eDalerFront200.png");

				this.EDalerGlyph = Url.ToString();
			}
			else if (!(this.Balance is null) && !(this.NeuronService.Wallet.LastBalance is null) &&
				(this.Balance.Amount != this.NeuronService.Wallet.LastBalance.Amount ||
				this.Balance.Currency != this.NeuronService.Wallet.LastBalance.Currency ||
				this.Balance.Timestamp != this.NeuronService.Wallet.LastBalance.Timestamp))
			{
				await this.ReloadWallet(this.NeuronService.Wallet.LastBalance);
			}

			EvaluateAllCommands();

			this.NeuronService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.NeuronService.Wallet.BalanceUpdated -= Wallet_BalanceUpdated;

			await base.DoUnbind();
		}

		private async Task AssignProperties(Balance Balance, decimal PendingAmount, string PendingCurrency, 
			EDaler.PendingPayment[] PendingPayments, EDaler.AccountEvent[] Events, bool More)
		{
			this.Balance = Balance;
			this.Amount = Balance.Amount;
			this.Currency = Balance.Currency;
			this.Timestamp = Balance.Timestamp;

			this.PendingAmount = PendingAmount;
			this.PendingCurrency = PendingCurrency;
			this.HasPending = (PendingPayments?.Length ?? 0) > 0;
			this.HasEvents = (Events?.Length ?? 0) > 0;
			this.HasMore = More;

			this.PendingPayments.Clear();
			if (!(PendingPayments is null))
			{
				foreach (EDaler.PendingPayment Payment in PendingPayments)
					this.PendingPayments.Add(new PendingPaymentItem(Payment));
			}

			this.Events.Clear();
			if (!(Events is null))
			{
				Dictionary<string, string> FriendlyNames = new Dictionary<string, string>();

				foreach (EDaler.AccountEvent Event in Events)
				{
					if (!FriendlyNames.TryGetValue(Event.Remote, out string FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote, this.NeuronService.Xmpp);
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
			Task.Run(() => this.ReloadWallet(e.Balance));
			return Task.CompletedTask;
		}

		private async Task ReloadWallet(Balance Balance)
		{
			try
			{
				(decimal PendingAmount, string PendingCurrency, EDaler.PendingPayment[] PendingPayments) = await this.NeuronService.Wallet.GetPendingPayments();
				(EDaler.AccountEvent[] Events, bool More) = await this.NeuronService.Wallet.GetAccountEventsAsync(50);

				this.UiSerializer.BeginInvokeOnMainThread(async () => await AssignProperties(Balance, PendingAmount, PendingCurrency, 
					PendingPayments, Events, More));
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
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
			BindableProperty.Create("Balance", typeof(Balance), typeof(MyWalletViewModel), default(Balance));

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
			BindableProperty.Create("Amount", typeof(decimal), typeof(MyWalletViewModel), default(decimal));

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
			BindableProperty.Create("Currency", typeof(string), typeof(MyWalletViewModel), default(string));

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
			BindableProperty.Create("HasPending", typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasPending of eDaler to process
		/// </summary>
		public bool HasPending
		{
			get { return (bool)GetValue(HasPendingProperty); }
			set { SetValue(HasPendingProperty, value); }
		}

		/// <summary>
		/// See <see cref="PendingAmount"/>
		/// </summary>
		public static readonly BindableProperty PendingAmountProperty =
			BindableProperty.Create("PendingAmount", typeof(decimal), typeof(MyWalletViewModel), default(decimal));

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
			BindableProperty.Create("PendingCurrency", typeof(string), typeof(MyWalletViewModel), default(string));

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
			BindableProperty.Create("Timestamp", typeof(DateTime), typeof(MyWalletViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Timestamp
		{
			get { return (DateTime)GetValue(TimestampProperty); }
			set { SetValue(TimestampProperty, value); }
		}

		/// <summary>
		/// See <see cref="EDalerGlyph"/>
		/// </summary>
		public static readonly BindableProperty EDalerGlyphProperty =
			BindableProperty.Create("EDalerGlyph", typeof(string), typeof(MyWalletViewModel), default(string));

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		public string EDalerGlyph
		{
			get { return (string)GetValue(EDalerGlyphProperty); }
			set { SetValue(EDalerGlyphProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasEvents"/>
		/// </summary>
		public static readonly BindableProperty HasEventsProperty =
			BindableProperty.Create("HasEvents", typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// HasEvents of eDaler to process
		/// </summary>
		public bool HasEvents
		{
			get { return (bool)GetValue(HasEventsProperty); }
			set { SetValue(HasEventsProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasMore"/>
		/// </summary>
		public static readonly BindableProperty HasMoreProperty =
			BindableProperty.Create("HasMore", typeof(bool), typeof(MyWalletViewModel), default(bool));

		/// <summary>
		/// If there are more account events available.
		/// </summary>
		public bool HasMore
		{
			get { return (bool)GetValue(HasMoreProperty); }
			set { SetValue(HasMoreProperty, value); }
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

		#endregion

		private async Task GoBack()
		{
			await this.navigationService.GoBackAsync();
		}

		private async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult(this.logService, this.NeuronService, this.navigationService,
				this.UiSerializer, this.contractOrchestratorService, this.thingRegistryOrchestratorService,
				this.eDalerOrchestratorService);
		}

		private async Task RequestPayment()
		{
			await this.navigationService.GoToAsync(nameof(Wallet.RequestPayment.RequestPaymentPage), new EDalerBalanceNavigationArgs(this.Balance));
		}

		private async Task MakePayment()
		{
			await this.navigationService.GoToAsync(nameof(MyContactsPage), 
				new ContactListNavigationArgs(AppResources.SelectContactToPay, SelectContactAction.MakePayment));
		}

		private async Task ShowPending(object P)
		{
			if (!(P is PendingPaymentItem Item))
				return;

			if (!this.NeuronService.Wallet.TryParseEDalerUri(Item.Uri, out EDalerUri Uri, out string Reason))
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.InvalidEDalerUri, Reason));
				return;
			}

			await this.navigationService.GoToAsync(nameof(PendingPayment.PendingPaymentPage), new EDalerUriNavigationArgs(Uri));
		}

		private async Task ShowEvent(object P)
		{
			if (!(P is AccountEventItem Item))
				return;

			await this.navigationService.GoToAsync(nameof(AccountEvent.AccountEventPage), new AccountEvent.AccountEventNavigationArgs(Item));
		}

	}
}