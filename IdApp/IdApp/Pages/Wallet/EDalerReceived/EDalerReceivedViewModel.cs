using System;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Waher.Networking.XMPP;

namespace IdApp.Pages.Wallet.EDalerReceived
{
	/// <summary>
	/// The view model to bind to for displaying information about an incoming balance change.
	/// </summary>
	public class EDalerReceivedViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="EDalerReceivedViewModel"/> class.
		/// </summary>
		public EDalerReceivedViewModel()
			: base()
		{
			this.AcceptCommand = new Command(async _ => await this.Accept(), _ => this.IsConnected);
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out EDalerBalanceNavigationArgs args))
			{
				this.Amount = args.Balance.Amount;
				this.Currency = args.Balance.Currency;
				this.Timestamp = args.Balance.Timestamp;

				if (args.Balance.Event is null)
				{
					this.HasId = false;
					this.HasFrom = false;
					this.HasMessage = false;
					this.HasChange = false;
				}
				else
				{
					this.Id = args.Balance.Event.TransactionId;
					this.From = args.Balance.Event.Remote;
					this.Message = args.Balance.Event.Message;
					this.Change = args.Balance.Event.Change;

					this.HasId = true;
					this.HasFrom = true;
					this.HasMessage = true;
					this.HasChange = true;
				}

				StringBuilder Url = new();

				Url.Append("https://");
				Url.Append(this.TagProfile.Domain);
				Url.Append("/Images/eDalerFront200.png");

				this.EDalerFrontGlyph = Url.ToString();

				Url.Clear();
				Url.Append("https://");
				Url.Append(this.TagProfile.Domain);
				Url.Append("/Images/eDalerBack200.png");

				this.EDalerBackGlyph = Url.ToString();
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.TagProfile.Changed += this.TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;
			await base.OnDispose();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.AcceptCommand);
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
			});

			return Task.CompletedTask;
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(EDalerReceivedViewModel), default(decimal));

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		public decimal Amount
		{
			get => (decimal)this.GetValue(AmountProperty);
			set => this.SetValue(AmountProperty, value);
		}

		/// <summary>
		/// See <see cref="Change"/>
		/// </summary>
		public static readonly BindableProperty ChangeProperty =
			BindableProperty.Create(nameof(Change), typeof(decimal), typeof(EDalerReceivedViewModel), default(decimal));

		/// <summary>
		/// Change in balance represented by current event.
		/// </summary>
		public decimal Change
		{
			get => (decimal)this.GetValue(ChangeProperty);
			set => this.SetValue(ChangeProperty, value);
		}

		/// <summary>
		/// See <see cref="HasChange"/>
		/// </summary>
		public static readonly BindableProperty HasChangeProperty =
			BindableProperty.Create(nameof(HasChange), typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// If <see cref="Change"/> is defined.
		/// </summary>
		public bool HasChange
		{
			get => (bool)this.GetValue(HasChangeProperty);
			set => this.SetValue(HasChangeProperty, value);
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create(nameof(Currency), typeof(string), typeof(EDalerReceivedViewModel), default(string));

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
			BindableProperty.Create(nameof(Timestamp), typeof(DateTime), typeof(EDalerReceivedViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Timestamp
		{
			get => (DateTime)this.GetValue(TimestampProperty);
			set => this.SetValue(TimestampProperty, value);
		}

		/// <summary>
		/// See <see cref="Id"/>
		/// </summary>
		public static readonly BindableProperty IdProperty =
			BindableProperty.Create(nameof(Id), typeof(Guid), typeof(EDalerReceivedViewModel), default(Guid));

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		public Guid Id
		{
			get => (Guid)this.GetValue(IdProperty);
			set => this.SetValue(IdProperty, value);
		}

		/// <summary>
		/// See <see cref="HasId"/>
		/// </summary>
		public static readonly BindableProperty HasIdProperty =
			BindableProperty.Create(nameof(HasId), typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// If <see cref="Id"/> is defined.
		/// </summary>
		public bool HasId
		{
			get => (bool)this.GetValue(HasIdProperty);
			set => this.SetValue(HasIdProperty, value);
		}

		/// <summary>
		/// See <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create(nameof(From), typeof(string), typeof(EDalerReceivedViewModel), default(string));

		/// <summary>
		/// From who eDaler is to be transferred
		/// </summary>
		public string From
		{
			get => (string)this.GetValue(FromProperty);
			set => this.SetValue(FromProperty, value);
		}

		/// <summary>
		/// See <see cref="HasFrom"/>
		/// </summary>
		public static readonly BindableProperty HasFromProperty =
			BindableProperty.Create(nameof(HasFrom), typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// If <see cref="From"/> is defined.
		/// </summary>
		public bool HasFrom
		{
			get => (bool)this.GetValue(HasFromProperty);
			set => this.SetValue(HasFromProperty, value);
		}

		/// <summary>
		/// See <see cref="Message"/>
		/// </summary>
		public static readonly BindableProperty MessageProperty =
			BindableProperty.Create(nameof(Message), typeof(string), typeof(EDalerReceivedViewModel), default(string));

		/// <summary>
		/// Message of eDaler to process
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
			BindableProperty.Create(nameof(HasMessage), typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// HasMessage of eDaler to process
		/// </summary>
		public bool HasMessage
		{
			get => (bool)this.GetValue(HasMessageProperty);
			set => this.SetValue(HasMessageProperty, value);
		}

		/// <summary>
		/// The command to bind to for Accepting a payment
		/// </summary>
		public ICommand AcceptCommand { get; }

		/// <summary>
		/// See <see cref="EDalerFrontGlyph"/>
		/// </summary>
		public static readonly BindableProperty EDalerFrontGlyphProperty =
			BindableProperty.Create(nameof(EDalerFrontGlyph), typeof(string), typeof(EDalerReceivedViewModel), default(string));

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
			BindableProperty.Create(nameof(EDalerBackGlyph), typeof(string), typeof(EDalerReceivedViewModel), default(string));

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		public string EDalerBackGlyph
		{
			get => (string)this.GetValue(EDalerBackGlyphProperty);
			set => this.SetValue(EDalerBackGlyphProperty, value);
		}

		#endregion

		private Task Accept()
		{
			return this.NavigationService.GoBackAsync();
		}
	}
}
