using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using EDaler;
using IdApp.Navigation;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.ViewModels.Wallet
{
	/// <summary>
	/// The view model to bind to for when displaying the wallet.
	/// </summary>
	public class MyWalletViewModel : NeuronViewModel
	{
		private readonly ITagProfile tagProfile;
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INetworkService networkService;

		/// <summary>
		/// Creates an instance of the <see cref="MyWalletViewModel"/> class.
		/// </summary>
		public MyWalletViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			INetworkService networkService,
			ILogService logService)
		: base(neuronService, uiDispatcher)
		{
			this.tagProfile = tagProfile;
			this.logService = logService;
			this.navigationService = navigationService;
			this.networkService = networkService;

			this.PendingPayments = new ObservableCollection<PendingPaymentItem>();
			this.Events = new ObservableCollection<AccountEventItem>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out WalletNavigationArgs args))
			{
				AssignProperties(args.Balance, args.PendingAmount, args.PendingCurrency, args.PendingPayments, args.Events, args.More);

				StringBuilder Url = new StringBuilder();

				Url.Append("https://");
				Url.Append(this.NeuronService.Xmpp.Host);
				Url.Append("/Images/eDalerFront200.png");

				this.EDalerGlyph = Url.ToString();
			}

			EvaluateAllCommands();

			this.NeuronService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.NeuronService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;

			await base.DoUnbind();
		}

		private void AssignProperties(Balance Balance, decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments,
			AccountEvent[] Events, bool More)
		{
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
				foreach (PendingPayment Payment in PendingPayments)
					this.PendingPayments.Add(new PendingPaymentItem(Payment));
			}

			this.Events.Clear();
			if (!(Events is null))
			{
				foreach (AccountEvent Event in Events)
					this.Events.Add(new AccountEventItem(Event, this));
			}
		}

		private void EvaluateAllCommands()
		{
		}

		private Task Wallet_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			this.ReloadWallet(e.Balance);
			return Task.CompletedTask;
		}

		private async void ReloadWallet(Balance Balance)
		{
			try
			{
				(decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments) = await this.NeuronService.Wallet.GetPendingPayments();
				(AccountEvent[] Events, bool More) = await this.NeuronService.Wallet.GetAccountEventsAsync(50);

				this.UiDispatcher.BeginInvokeOnMainThread(() => AssignProperties(Balance, PendingAmount, PendingCurrency, PendingPayments,
					Events, More));
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		#region Properties

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create("Amount", typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

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
			BindableProperty.Create("Currency", typeof(string), typeof(EDalerUriViewModel), default(string));

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
			BindableProperty.Create("HasPending", typeof(bool), typeof(EDalerUriViewModel), default(bool));

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
			BindableProperty.Create("PendingAmount", typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

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
			BindableProperty.Create("PendingCurrency", typeof(string), typeof(EDalerUriViewModel), default(string));

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
			BindableProperty.Create("Timestamp", typeof(DateTime), typeof(EDalerUriViewModel), default(DateTime));

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
			BindableProperty.Create("EDalerGlyph", typeof(string), typeof(EDalerUriViewModel), default(string));

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
			BindableProperty.Create("HasEvents", typeof(bool), typeof(EDalerUriViewModel), default(bool));

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
			BindableProperty.Create("HasMore", typeof(bool), typeof(EDalerUriViewModel), default(bool));

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

		#endregion

	}
}