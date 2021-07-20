using System;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.EDalerReceived
{
	/// <summary>
	/// The view model to bind to for displaying information about an incoming balance change.
	/// </summary>
	public class EDalerReceivedViewModel : NeuronViewModel
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public EDalerReceivedViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService)
		: base(neuronService, uiDispatcher, tagProfile)
		{
			this.navigationService = navigationService;

			this.AcceptCommand = new Command(async _ => await Accept(), _ => IsConnected);
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out EDalerBalanceNavigationArgs args))
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

				StringBuilder Url = new StringBuilder();

				Url.Append("https://");
				Url.Append(this.NeuronService.Xmpp.Host);
				Url.Append("/Images/eDalerFront200.png");

				this.EDalerGlyph = Url.ToString();
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
			this.EvaluateCommands(this.AcceptCommand);
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

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create("Amount", typeof(decimal), typeof(EDalerReceivedViewModel), default(decimal));

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		public decimal Amount
		{
			get { return (decimal)GetValue(AmountProperty); }
			set { SetValue(AmountProperty, value); }
		}

		/// <summary>
		/// See <see cref="Change"/>
		/// </summary>
		public static readonly BindableProperty ChangeProperty =
			BindableProperty.Create("Change", typeof(decimal), typeof(EDalerReceivedViewModel), default(decimal));

		/// <summary>
		/// Change in balance represented by current event.
		/// </summary>
		public decimal Change
		{
			get { return (decimal)GetValue(ChangeProperty); }
			set { SetValue(ChangeProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasChange"/>
		/// </summary>
		public static readonly BindableProperty HasChangeProperty =
			BindableProperty.Create("HasChange", typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// If <see cref="Change"/> is defined.
		/// </summary>
		public bool HasChange
		{
			get { return (bool)GetValue(HasChangeProperty); }
			set { SetValue(HasChangeProperty, value); }
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create("Currency", typeof(string), typeof(EDalerReceivedViewModel), default(string));

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
			BindableProperty.Create("Timestamp", typeof(DateTime), typeof(EDalerReceivedViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Timestamp
		{
			get { return (DateTime)GetValue(TimestampProperty); }
			set { SetValue(TimestampProperty, value); }
		}

		/// <summary>
		/// See <see cref="Id"/>
		/// </summary>
		public static readonly BindableProperty IdProperty =
			BindableProperty.Create("Id", typeof(Guid), typeof(EDalerReceivedViewModel), default(Guid));

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		public Guid Id
		{
			get { return (Guid)GetValue(IdProperty); }
			set { SetValue(IdProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasId"/>
		/// </summary>
		public static readonly BindableProperty HasIdProperty =
			BindableProperty.Create("HasId", typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// If <see cref="Id"/> is defined.
		/// </summary>
		public bool HasId
		{
			get { return (bool)GetValue(HasIdProperty); }
			set { SetValue(HasIdProperty, value); }
		}

		/// <summary>
		/// See <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create("From", typeof(string), typeof(EDalerReceivedViewModel), default(string));

		/// <summary>
		/// From who eDaler is to be transferred
		/// </summary>
		public string From
		{
			get { return (string)GetValue(FromProperty); }
			set { SetValue(FromProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasFrom"/>
		/// </summary>
		public static readonly BindableProperty HasFromProperty =
			BindableProperty.Create("HasFrom", typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// If <see cref="From"/> is defined.
		/// </summary>
		public bool HasFrom
		{
			get { return (bool)GetValue(HasFromProperty); }
			set { SetValue(HasFromProperty, value); }
		}

		/// <summary>
		/// See <see cref="Message"/>
		/// </summary>
		public static readonly BindableProperty MessageProperty =
			BindableProperty.Create("Message", typeof(string), typeof(EDalerReceivedViewModel), default(string));

		/// <summary>
		/// Message of eDaler to process
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
			BindableProperty.Create("HasMessage", typeof(bool), typeof(EDalerReceivedViewModel), default(bool));

		/// <summary>
		/// HasMessage of eDaler to process
		/// </summary>
		public bool HasMessage
		{
			get { return (bool)GetValue(HasMessageProperty); }
			set { SetValue(HasMessageProperty, value); }
		}

		/// <summary>
		/// The command to bind to for claiming a thing
		/// </summary>
		public ICommand AcceptCommand { get; }

		/// <summary>
		/// See <see cref="EDalerGlyph"/>
		/// </summary>
		public static readonly BindableProperty EDalerGlyphProperty =
			BindableProperty.Create("EDalerGlyph", typeof(string), typeof(EDalerReceivedViewModel), default(string));

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		public string EDalerGlyph
		{
			get { return (string)GetValue(EDalerGlyphProperty); }
			set { SetValue(EDalerGlyphProperty, value); }
		}

		#endregion

		private Task Accept()
		{
			return this.navigationService.GoBackAsync();
		}
	}
}