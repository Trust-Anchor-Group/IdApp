using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Converters;
using IdApp.Services.Xmpp;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.AccountEvent
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of an account event.
	/// </summary>
	public class AccountEventViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public AccountEventViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out AccountEventNavigationArgs args))
			{
				this.Remote = args.Event.Remote;
				this.FriendlyName = args.Event.FriendlyName;
				this.Timestamp = args.Event.Timestamp;
				this.TimestampStr = args.Event.TimestampStr;
				this.Change = args.Event.Change;
				this.ChangeColor = args.Event.TextColor;
				this.Balance = args.Event.Balance;
				this.Message = args.Event.Message;
				this.HasMessage = args.Event.HasMessage;
				this.Id = args.Event.TransactionId.ToString();
				this.Currency = args.Event.Currency;

				this.ChangeText = MoneyToString.ToString(this.Change);
				this.ChangeAndCurrency = this.ChangeText + " " + this.Currency;

				this.BalanceText = MoneyToString.ToString(this.Balance);
				this.BalanceAndCurrency = this.BalanceText + " " + this.Currency;
			}

			AssignProperties();
			EvaluateAllCommands();

			this.TagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.TagProfile.Changed -= TagProfile_Changed;
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
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

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Change"/>
		/// </summary>
		public static readonly BindableProperty ChangeProperty =
			BindableProperty.Create("Change", typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

		/// <summary>
		/// Change of eDaler
		/// </summary>
		public decimal Change
		{
			get { return (decimal)GetValue(ChangeProperty); }
			set { SetValue(ChangeProperty, value); }
		}

		/// <summary>
		/// See <see cref="ChangeColor"/>
		/// </summary>
		public static readonly BindableProperty ChangeColorProperty =
			BindableProperty.Create("ChangeColor", typeof(Color), typeof(EDalerUriViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="Change"/> field.
		/// </summary>
		public Color ChangeColor
		{
			get { return (Color)GetValue(ChangeColorProperty); }
			set { SetValue(ChangeColorProperty, value); }
		}

		/// <summary>
		/// See <see cref="ChangeText"/>
		/// </summary>
		public static readonly BindableProperty ChangeTextProperty =
			BindableProperty.Create("ChangeText", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="Change"/> as text.
		/// </summary>
		public string ChangeText
		{
			get { return (string)GetValue(ChangeTextProperty); }
			set { SetValue(ChangeTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="ChangeAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty ChangeAndCurrencyProperty =
			BindableProperty.Create("ChangeAndCurrency", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="ChangeText"/> and <see cref="Currency"/>.
		/// </summary>
		public string ChangeAndCurrency
		{
			get { return (string)GetValue(ChangeAndCurrencyProperty); }
			set { SetValue(ChangeAndCurrencyProperty, value); }
		}

		/// <summary>
		/// See <see cref="Balance"/>
		/// </summary>
		public static readonly BindableProperty BalanceProperty =
			BindableProperty.Create("Balance", typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

		/// <summary>
		/// Balance of eDaler
		/// </summary>
		public decimal Balance
		{
			get { return (decimal)GetValue(BalanceProperty); }
			set { SetValue(BalanceProperty, value); }
		}

		/// <summary>
		/// See <see cref="BalanceText"/>
		/// </summary>
		public static readonly BindableProperty BalanceTextProperty =
			BindableProperty.Create("BalanceText", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="Balance"/> as text.
		/// </summary>
		public string BalanceText
		{
			get { return (string)GetValue(BalanceTextProperty); }
			set { SetValue(BalanceTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="BalanceAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty BalanceAndCurrencyProperty =
			BindableProperty.Create("BalanceAndCurrency", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="BalanceText"/> and <see cref="Currency"/>.
		/// </summary>
		public string BalanceAndCurrency
		{
			get { return (string)GetValue(BalanceAndCurrencyProperty); }
			set { SetValue(BalanceAndCurrencyProperty, value); }
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
		/// See <see cref="TimestampStr"/>
		/// </summary>
		public static readonly BindableProperty TimestampStrProperty =
			BindableProperty.Create("TimestampStr", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// When code expires
		/// </summary>
		public string TimestampStr
		{
			get { return (string)GetValue(TimestampStrProperty); }
			set { SetValue(TimestampStrProperty, value); }
		}

		/// <summary>
		/// See <see cref="Id"/>
		/// </summary>
		public static readonly BindableProperty IdProperty =
			BindableProperty.Create("Id", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		public string Id
		{
			get { return (string)GetValue(IdProperty); }
			set { SetValue(IdProperty, value); }
		}

		/// <summary>
		/// See <see cref="Remote"/>
		/// </summary>
		public static readonly BindableProperty RemoteProperty =
			BindableProperty.Create("Remote", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Remote who eDaler is to be transferred
		/// </summary>
		public string Remote
		{
			get { return (string)GetValue(RemoteProperty); }
			set { SetValue(RemoteProperty, value); }
		}

		/// <summary>
		/// See <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create("FriendlyName", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// FriendlyName who eDaler is to be transferred
		/// </summary>
		public string FriendlyName
		{
			get { return (string)GetValue(FriendlyNameProperty); }
			set { SetValue(FriendlyNameProperty, value); }
		}

		/// <summary>
		/// See <see cref="Message"/>
		/// </summary>
		public static readonly BindableProperty MessageProperty =
			BindableProperty.Create("Message", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Message to recipient
		/// </summary>
		public string Message
		{
			get { return (string)GetValue(MessageProperty); }
			set { SetValue(MessageProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasMessage"/>
		/// </summary>
		public static readonly BindableProperty HasMessageProperty =
			BindableProperty.Create("HasMessage", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If a message is defined
		/// </summary>
		public bool HasMessage
		{
			get { return (bool)GetValue(HasMessageProperty); }
			set { SetValue(HasMessageProperty, value); }
		}

		#endregion

	}
}