﻿using System;
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
			BindableProperty.Create(nameof(Change), typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

		/// <summary>
		/// Change of eDaler
		/// </summary>
		public decimal Change
		{
			get => (decimal)this.GetValue(ChangeProperty);
			set => this.SetValue(ChangeProperty, value);
		}

		/// <summary>
		/// See <see cref="ChangeColor"/>
		/// </summary>
		public static readonly BindableProperty ChangeColorProperty =
			BindableProperty.Create(nameof(ChangeColor), typeof(Color), typeof(EDalerUriViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="Change"/> field.
		/// </summary>
		public Color ChangeColor
		{
			get => (Color)this.GetValue(ChangeColorProperty);
			set => this.SetValue(ChangeColorProperty, value);
		}

		/// <summary>
		/// See <see cref="ChangeText"/>
		/// </summary>
		public static readonly BindableProperty ChangeTextProperty =
			BindableProperty.Create(nameof(ChangeText), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="Change"/> as text.
		/// </summary>
		public string ChangeText
		{
			get => (string)this.GetValue(ChangeTextProperty);
			set => this.SetValue(ChangeTextProperty, value);
		}

		/// <summary>
		/// See <see cref="ChangeAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty ChangeAndCurrencyProperty =
			BindableProperty.Create(nameof(ChangeAndCurrency), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="ChangeText"/> and <see cref="Currency"/>.
		/// </summary>
		public string ChangeAndCurrency
		{
			get => (string)this.GetValue(ChangeAndCurrencyProperty);
			set => this.SetValue(ChangeAndCurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="Balance"/>
		/// </summary>
		public static readonly BindableProperty BalanceProperty =
			BindableProperty.Create(nameof(Balance), typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

		/// <summary>
		/// Balance of eDaler
		/// </summary>
		public decimal Balance
		{
			get => (decimal)this.GetValue(BalanceProperty);
			set => this.SetValue(BalanceProperty, value);
		}

		/// <summary>
		/// See <see cref="BalanceText"/>
		/// </summary>
		public static readonly BindableProperty BalanceTextProperty =
			BindableProperty.Create(nameof(BalanceText), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="Balance"/> as text.
		/// </summary>
		public string BalanceText
		{
			get => (string)this.GetValue(BalanceTextProperty);
			set => this.SetValue(BalanceTextProperty, value);
		}

		/// <summary>
		/// See <see cref="BalanceAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty BalanceAndCurrencyProperty =
			BindableProperty.Create(nameof(BalanceAndCurrency), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="BalanceText"/> and <see cref="Currency"/>.
		/// </summary>
		public string BalanceAndCurrency
		{
			get => (string)this.GetValue(BalanceAndCurrencyProperty);
			set => this.SetValue(BalanceAndCurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create(nameof(Currency), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Currency of eDaler to process
		/// </summary>
		public string Currency
		{
			get => (string)this.GetValue(CurrencyProperty);
			set => this.SetValue(CurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="Timestamp"/>
		/// </summary>
		public static readonly BindableProperty TimestampProperty =
			BindableProperty.Create(nameof(Timestamp), typeof(DateTime), typeof(EDalerUriViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Timestamp
		{
			get => (DateTime)this.GetValue(TimestampProperty);
			set => this.SetValue(TimestampProperty, value);
		}

		/// <summary>
		/// See <see cref="TimestampStr"/>
		/// </summary>
		public static readonly BindableProperty TimestampStrProperty =
			BindableProperty.Create(nameof(TimestampStr), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// When code expires
		/// </summary>
		public string TimestampStr
		{
			get => (string)this.GetValue(TimestampStrProperty);
			set => this.SetValue(TimestampStrProperty, value);
		}

		/// <summary>
		/// See <see cref="Id"/>
		/// </summary>
		public static readonly BindableProperty IdProperty =
			BindableProperty.Create(nameof(Id), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		public string Id
		{
			get => (string)this.GetValue(IdProperty);
			set => this.SetValue(IdProperty, value);
		}

		/// <summary>
		/// See <see cref="Remote"/>
		/// </summary>
		public static readonly BindableProperty RemoteProperty =
			BindableProperty.Create(nameof(Remote), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Remote who eDaler is to be transferred
		/// </summary>
		public string Remote
		{
			get => (string)this.GetValue(RemoteProperty);
			set => this.SetValue(RemoteProperty, value);
		}

		/// <summary>
		/// See <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create(nameof(FriendlyName), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// FriendlyName who eDaler is to be transferred
		/// </summary>
		public string FriendlyName
		{
			get => (string)this.GetValue(FriendlyNameProperty);
			set => this.SetValue(FriendlyNameProperty, value);
		}

		/// <summary>
		/// See <see cref="Message"/>
		/// </summary>
		public static readonly BindableProperty MessageProperty =
			BindableProperty.Create(nameof(Message), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Message to recipient
		/// </summary>
		public string Message
		{
			get => (string)this.GetValue(MessageProperty);
			set => this.SetValue(MessageProperty, value);
		}

		/// <summary>
		/// See <see cref="HasMessage"/>
		/// </summary>
		public static readonly BindableProperty HasMessageProperty =
			BindableProperty.Create(nameof(HasMessage), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If a message is defined
		/// </summary>
		public bool HasMessage
		{
			get => (bool)this.GetValue(HasMessageProperty);
			set => this.SetValue(HasMessageProperty, value);
		}

		#endregion

	}
}