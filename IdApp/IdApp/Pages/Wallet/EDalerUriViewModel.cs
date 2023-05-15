using System;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using IdApp.DeviceSpecific;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;
using IdApp.Converters;
using IdApp.Pages.Main.Calculator;
using Xamarin.CommunityToolkit.Helpers;
using Waher.Networking.XMPP.StanzaErrors;

namespace IdApp.Pages.Wallet
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of an eDaler URI.
	/// </summary>
	public class EDalerUriViewModel : QrXmppViewModel
	{
		private readonly IShareQrCode shareQrCode;
		private TaskCompletionSource<string> uriToSend;

		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public EDalerUriViewModel(IShareQrCode ShareQrCode)
		: base()
		{
			this.shareQrCode = ShareQrCode;
			this.uriToSend = null;

			this.AcceptCommand = new Command(async _ => await this.Accept(), _ => this.IsConnected);
			this.GenerateQrCodeCommand = new Command(async _ => await this.GenerateQrCode(), _ => this.CanGenerateQrCode());
			this.PayOnlineCommand = new Command(async _ => await this.PayOnline(), _ => this.CanPayOnline());
			this.ShareCommand = new Command(async _ => await this.Share(), _ => this.CanShare());
			this.SubmitCommand = new Command(async _ => await this.Submit(), _ => this.IsConnected);
			this.ShowCodeCommand = new Command(async _ => await this.ShowCode());
			this.SendPaymentCommand = new Command(async _ => await this.SendPayment(), _ => this.CanSendPayment());
			this.OpenCalculatorCommand = new Command(async P => await this.OpenCalculator(P));

			this.FromClickCommand = new Command(async x => await this.FromLabelClicked());
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out EDalerUriNavigationArgs args))
			{
				this.uriToSend = args.UriToSend;

				if (!args.ViewInitialized)
				{
					this.FriendlyName = args.FriendlyName;

					if (args.Uri is not null)
					{
						this.Uri = args.Uri.UriString;
						this.Id = args.Uri.Id;
						this.Amount = args.Uri.Amount;
						this.AmountExtra = args.Uri.AmountExtra;
						this.Currency = args.Uri.Currency;
						this.Created = args.Uri.Created;
						this.Expires = args.Uri.Expires;
						this.ExpiresStr = this.Expires.ToShortDateString();
						this.From = args.Uri.From;
						this.FromType = args.Uri.FromType;
						this.To = args.Uri.To;
						this.ToType = args.Uri.ToType;
						this.ToPreset = !string.IsNullOrEmpty(args.Uri.To);
						this.Complete = args.Uri.Complete;
					}

					this.RemoveQrCode();
					this.NotPaid = true;

					this.AmountText = this.Amount <= 0 ? string.Empty : MoneyToString.ToString(this.Amount);
					this.AmountOk = CommonTypes.TryParse(this.AmountText, out decimal d) && d > 0;
					this.AmountPreset = !string.IsNullOrEmpty(this.AmountText) && this.AmountOk;
					this.AmountAndCurrency = this.AmountText + " " + this.Currency;

					this.AmountExtraText = this.AmountExtra.HasValue ? MoneyToString.ToString(this.AmountExtra.Value) : string.Empty;
					this.AmountExtraOk = !this.AmountExtra.HasValue || this.AmountExtra.Value >= 0;
					this.AmountExtraPreset = this.AmountExtra.HasValue;
					this.AmountExtraAndCurrency = this.AmountExtraText + " " + this.Currency;

					StringBuilder Url = new();

					Url.Append("https://");
					Url.Append(this.From);
					Url.Append("/Images/eDalerFront200.png");

					this.EDalerFrontGlyph = Url.ToString();

					Url.Clear();
					Url.Append("https://");
					Url.Append(this.TagProfile.Domain);
					Url.Append("/Images/eDalerBack200.png");

					this.EDalerBackGlyph = Url.ToString();

					if (args.Uri?.EncryptedMessage is not null)
					{
						if (args.Uri.EncryptionPublicKey is null)
							this.Message = Encoding.UTF8.GetString(args.Uri.EncryptedMessage);
						else
						{
							this.Message = await this.XmppService.TryDecryptMessage(args.Uri.EncryptedMessage,
								args.Uri.EncryptionPublicKey, args.Uri.Id, args.Uri.From);
						}

						this.HasMessage = !string.IsNullOrEmpty(this.Message);
					}

					this.MessagePreset = !string.IsNullOrEmpty(this.Message);
					this.EncryptMessage = args.Uri?.ToType == EntityType.LegalId;

					args.ViewInitialized = true;
				}
			}

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.TagProfile.Changed += this.TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;

			this.uriToSend?.TrySetResult(null);

			await base.OnDispose();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.AcceptCommand, this.PayOnlineCommand, this.GenerateQrCodeCommand, this.ShareCommand,
				this.SubmitCommand, this.ShareCommand, this.SendPaymentCommand, this.OpenCalculatorCommand);
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
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create(nameof(Uri), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// edaler URI to process
		/// </summary>
		public string Uri
		{
			get => (string)this.GetValue(UriProperty);
			set => this.SetValue(UriProperty, value);
		}

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(EDalerUriViewModel), default(decimal));

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		public decimal Amount
		{
			get => (decimal)this.GetValue(AmountProperty);
			set => this.SetValue(AmountProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountOk"/>
		/// </summary>
		public static readonly BindableProperty AmountOkProperty =
			BindableProperty.Create(nameof(AmountOk), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Amount"/> is OK.
		/// </summary>
		public bool AmountOk
		{
			get => (bool)this.GetValue(AmountOkProperty);
			set => this.SetValue(AmountOkProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountColor"/>
		/// </summary>
		public static readonly BindableProperty AmountColorProperty =
			BindableProperty.Create(nameof(AmountColor), typeof(Color), typeof(EDalerUriViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="Amount"/> field.
		/// </summary>
		public Color AmountColor
		{
			get => (Color)this.GetValue(AmountColorProperty);
			set => this.SetValue(AmountColorProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountText"/>
		/// </summary>
		public static readonly BindableProperty AmountTextProperty =
			BindableProperty.Create(nameof(AmountText), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="Amount"/> as text.
		/// </summary>
		public string AmountText
		{
			get => (string)this.GetValue(AmountTextProperty);
			set
			{
				this.SetValue(AmountTextProperty, value);

				if (CommonTypes.TryParse(value, out decimal d) && d > 0)
				{
					this.Amount = d;
					this.AmountOk = true;
					this.AmountColor = Color.Default;
				}
				else
				{
					this.AmountOk = false;
					this.AmountColor = Color.Salmon;
				}

				this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand, this.SendPaymentCommand);
			}
		}

		/// <summary>
		/// See <see cref="AmountAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty AmountAndCurrencyProperty =
			BindableProperty.Create(nameof(AmountAndCurrency), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="AmountText"/> and <see cref="Currency"/>.
		/// </summary>
		public string AmountAndCurrency
		{
			get => (string)this.GetValue(AmountAndCurrencyProperty);
			set => this.SetValue(AmountAndCurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountPreset"/>
		/// </summary>
		public static readonly BindableProperty AmountPresetProperty =
			BindableProperty.Create(nameof(AmountPreset), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Amount"/> is preset.
		/// </summary>
		public bool AmountPreset
		{
			get => (bool)this.GetValue(AmountPresetProperty);
			set => this.SetValue(AmountPresetProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountExtra"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraProperty =
			BindableProperty.Create(nameof(AmountExtra), typeof(decimal?), typeof(EDalerUriViewModel), default(decimal?));

		/// <summary>
		/// AmountExtra of eDaler to process
		/// </summary>
		public decimal? AmountExtra
		{
			get { return (decimal?)this.GetValue(AmountExtraProperty); }
			set => this.SetValue(AmountExtraProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountExtraOk"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraOkProperty =
			BindableProperty.Create(nameof(AmountExtraOk), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="AmountExtra"/> is OK.
		/// </summary>
		public bool AmountExtraOk
		{
			get => (bool)this.GetValue(AmountExtraOkProperty);
			set => this.SetValue(AmountExtraOkProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountExtraColor"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraColorProperty =
			BindableProperty.Create(nameof(AmountExtraColor), typeof(Color), typeof(EDalerUriViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="AmountExtra"/> field.
		/// </summary>
		public Color AmountExtraColor
		{
			get => (Color)this.GetValue(AmountExtraColorProperty);
			set => this.SetValue(AmountExtraColorProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountExtraText"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraTextProperty =
			BindableProperty.Create(nameof(AmountExtraText), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="AmountExtra"/> as text.
		/// </summary>
		public string AmountExtraText
		{
			get => (string)this.GetValue(AmountExtraTextProperty);
			set
			{
				this.SetValue(AmountExtraTextProperty, value);

				if (string.IsNullOrEmpty(value))
				{
					this.AmountExtra = null;
					this.AmountExtraOk = true;
					this.AmountExtraColor = Color.Default;
				}
				else if (CommonTypes.TryParse(value, out decimal d) && d >= 0)
				{
					this.AmountExtra = d;
					this.AmountExtraOk = true;
					this.AmountExtraColor = Color.Default;
				}
				else
				{
					this.AmountExtraOk = false;
					this.AmountExtraColor = Color.Salmon;
				}

				this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand, this.SendPaymentCommand);
			}
		}

		/// <summary>
		/// See <see cref="AmountExtraAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraAndCurrencyProperty =
			BindableProperty.Create(nameof(AmountExtraAndCurrency), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="AmountExtraText"/> and <see cref="Currency"/>.
		/// </summary>
		public string AmountExtraAndCurrency
		{
			get => (string)this.GetValue(AmountExtraAndCurrencyProperty);
			set => this.SetValue(AmountExtraAndCurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountExtraPreset"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraPresetProperty =
			BindableProperty.Create(nameof(AmountExtraPreset), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="AmountExtra"/> is preset.
		/// </summary>
		public bool AmountExtraPreset
		{
			get => (bool)this.GetValue(AmountExtraPresetProperty);
			set => this.SetValue(AmountExtraPresetProperty, value);
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
		/// See <see cref="Created"/>
		/// </summary>
		public static readonly BindableProperty CreatedProperty =
			BindableProperty.Create(nameof(Created), typeof(DateTime), typeof(EDalerUriViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Created
		{
			get => (DateTime)this.GetValue(CreatedProperty);
			set => this.SetValue(CreatedProperty, value);
		}

		/// <summary>
		/// See <see cref="Expires"/>
		/// </summary>
		public static readonly BindableProperty ExpiresProperty =
			BindableProperty.Create(nameof(Expires), typeof(DateTime), typeof(EDalerUriViewModel), default(DateTime));

		/// <summary>
		/// When code expires
		/// </summary>
		public DateTime Expires
		{
			get => (DateTime)this.GetValue(ExpiresProperty);
			set => this.SetValue(ExpiresProperty, value);
		}

		/// <summary>
		/// See <see cref="ExpiresStr"/>
		/// </summary>
		public static readonly BindableProperty ExpiresStrProperty =
			BindableProperty.Create(nameof(ExpiresStr), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// When code expires
		/// </summary>
		public string ExpiresStr
		{
			get => (string)this.GetValue(ExpiresStrProperty);
			set => this.SetValue(ExpiresStrProperty, value);
		}

		/// <summary>
		/// See <see cref="Id"/>
		/// </summary>
		public static readonly BindableProperty IdProperty =
			BindableProperty.Create(nameof(Id), typeof(Guid), typeof(EDalerUriViewModel), default(Guid));

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		public Guid Id
		{
			get => (Guid)this.GetValue(IdProperty);
			set => this.SetValue(IdProperty, value);
		}

		/// <summary>
		/// See <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create(nameof(From), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// From who eDaler is to be transferred
		/// </summary>
		public string From
		{
			get => (string)this.GetValue(FromProperty);
			set => this.SetValue(FromProperty, value);
		}

		/// <summary>
		/// See <see cref="FromType"/>
		/// </summary>
		public static readonly BindableProperty FromTypeProperty =
			BindableProperty.Create(nameof(FromType), typeof(EntityType), typeof(EDalerUriViewModel), default(EntityType));

		/// <summary>
		/// Type of identity specified in <see cref="From"/>
		/// </summary>
		public EntityType FromType
		{
			get => (EntityType)this.GetValue(FromTypeProperty);
			set => this.SetValue(FromTypeProperty, value);
		}

		/// <summary>
		/// See <see cref="To"/>
		/// </summary>
		public static readonly BindableProperty ToProperty =
			BindableProperty.Create(nameof(To), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// To whom eDaler is to be transferred
		/// </summary>
		public string To
		{
			get => (string)this.GetValue(ToProperty);
			set => this.SetValue(ToProperty, value);
		}

		/// <summary>
		/// See <see cref="ToPreset"/>
		/// </summary>
		public static readonly BindableProperty ToPresetProperty =
			BindableProperty.Create(nameof(ToPreset), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="To"/> is preset
		/// </summary>
		public bool ToPreset
		{
			get => (bool)this.GetValue(ToPresetProperty);
			set => this.SetValue(ToPresetProperty, value);
		}

		/// <summary>
		/// See <see cref="ToType"/>
		/// </summary>
		public static readonly BindableProperty ToTypeProperty =
			BindableProperty.Create(nameof(ToType), typeof(EntityType), typeof(EDalerUriViewModel), default(EntityType));

		/// <summary>
		/// Type of identity specified in <see cref="To"/>
		/// </summary>
		public EntityType ToType
		{
			get => (EntityType)this.GetValue(ToTypeProperty);
			set => this.SetValue(ToTypeProperty, value);
		}

		/// <summary>
		/// See <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create(nameof(FriendlyName), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Optional FriendlyName associated with URI
		/// </summary>
		public string FriendlyName
		{
			get => (string)this.GetValue(FriendlyNameProperty);
			set => this.SetValue(FriendlyNameProperty, value);
		}

		/// <summary>
		/// See <see cref="Complete"/>
		/// </summary>
		public static readonly BindableProperty CompleteProperty =
			BindableProperty.Create(nameof(Complete), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If the URI is complete or not.
		/// </summary>
		public bool Complete
		{
			get => (bool)this.GetValue(CompleteProperty);
			set => this.SetValue(CompleteProperty, value);
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
		/// See <see cref="EncryptMessage"/>
		/// </summary>
		public static readonly BindableProperty EncryptMessageProperty =
			BindableProperty.Create(nameof(EncryptMessage), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		public bool EncryptMessage
		{
			get => (bool)this.GetValue(EncryptMessageProperty);
			set => this.SetValue(EncryptMessageProperty, value);
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

		/// <summary>
		/// See <see cref="MessagePreset"/>
		/// </summary>
		public static readonly BindableProperty MessagePresetProperty =
			BindableProperty.Create(nameof(MessagePreset), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Message"/> is preset.
		/// </summary>
		public bool MessagePreset
		{
			get => (bool)this.GetValue(MessagePresetProperty);
			set => this.SetValue(MessagePresetProperty, value);
		}

		/// <summary>
		/// The command to bind to for accepting the URI
		/// </summary>
		public ICommand AcceptCommand { get; }

		/// <summary>
		/// Gets or sets whether a user can accept the URI or not.
		/// </summary>
		public bool CanAccept
		{
			get { return this.XmppService.State == XmppState.Connected; }
		}

		/// <summary>
		/// See <see cref="CanAccept"/>
		/// </summary>
		public static readonly BindableProperty CanAcceptProperty =
			BindableProperty.Create(nameof(CanAccept), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// See <see cref="NotPaid"/>
		/// </summary>
		public static readonly BindableProperty NotPaidProperty =
			BindableProperty.Create(nameof(NotPaid), typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If the URI is complete or not.
		/// </summary>
		public bool NotPaid
		{
			get => (bool)this.GetValue(NotPaidProperty);
			set => this.SetValue(NotPaidProperty, value);
		}

		/// <summary>
		/// The command to bind to for paying online
		/// </summary>
		public ICommand PayOnlineCommand { get; }

		/// <summary>
		/// The command to bind to for generating a QR code
		/// </summary>
		public ICommand GenerateQrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sharing the QR code
		/// </summary>
		public ICommand ShareCommand { get; }

		/// <summary>
		/// The command to bind to for resubmitting a payment.
		/// </summary>
		public ICommand SubmitCommand { get; }

		/// <summary>
		/// The command to bind to for displaying eDaler URI as a QR Code
		/// </summary>
		public ICommand ShowCodeCommand { get; }

		/// <summary>
		/// The command to bind to send the payment via the parent page.
		/// </summary>
		public ICommand SendPaymentCommand { get; }

		/// <summary>
		/// Command to bind to for detecting when a from label has been clicked on.
		/// </summary>
		public ICommand FromClickCommand { get; }

		/// <summary>
		/// The command to bind to open the calculator.
		/// </summary>
		public ICommand OpenCalculatorCommand { get; }

		/// <summary>
		/// See <see cref="EDalerFrontGlyph"/>
		/// </summary>
		public static readonly BindableProperty EDalerFrontGlyphProperty =
			BindableProperty.Create(nameof(EDalerFrontGlyph), typeof(string), typeof(EDalerUriViewModel), default(string));

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
			BindableProperty.Create(nameof(EDalerBackGlyph), typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		public string EDalerBackGlyph
		{
			get => (string)this.GetValue(EDalerBackGlyphProperty);
			set => this.SetValue(EDalerBackGlyphProperty, value);
		}

		#endregion

		private async Task FromLabelClicked()
		{
			try
			{
				string Value = this.From;

				if ((Value.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) ||
					Value.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase)) &&
					System.Uri.TryCreate(Value, UriKind.Absolute, out Uri Uri) && await Launcher.TryOpenAsync(Uri))
				{
					return;
				}

				if (System.Uri.TryCreate("https://" + Value, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
					return;

				await Clipboard.SetTextAsync(Value);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TagValueCopiedToClipboard"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Accept()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				(bool succeeded, Transaction Transaction) = await this.NetworkService.TryRequest(() => this.XmppService.SendEDalerUri(this.Uri));
				if (succeeded)
				{
					await this.NavigationService.GoBackAsync();
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TransactionAccepted"]);
				}
				else
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToProcessEDalerUri"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task PayOnline()
		{
			try
			{
				if (!this.NotPaid)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PaymentAlreadySent"]);
					return;
				}

				if (!await App.VerifyPin())
					return;

				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					try
					{
						LegalIdentity LegalIdentity = await this.XmppService.GetLegalIdentity(this.To);
						Uri = await this.XmppService.CreateFullEDalerPaymentUri(LegalIdentity, this.Amount, this.AmountExtra,
							this.Currency, 3, this.Message);
					}
					catch (ForbiddenException)
					{
						// This happens if you try to view someone else's legal identity.
						// When this happens, try to send a petition to view it instead.
						// Normal operation. Should not be logged.

						this.NotPaid = true;
						this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand, this.SendPaymentCommand);

						this.UiSerializer.BeginInvokeOnMainThread(async () =>
						{
							bool Succeeded = await this.NetworkService.TryRequest(() => this.XmppService.PetitionIdentity(
								this.To, Guid.NewGuid().ToString(), LocalizationResourceManager.Current["EncryptedPayment"]));

							if (Succeeded)
							{
								await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PetitionSent"],
									LocalizationResourceManager.Current["APetitionHasBeenSentForEncryption"]);
							}
						});

						return;
					}
				}
				else
				{
					Uri = await this.XmppService.CreateFullEDalerPaymentUri(this.To, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				this.NotPaid = false;
				this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand, this.SendPaymentCommand);

				(bool succeeded, Transaction Transaction) = await this.NetworkService.TryRequest(() => this.XmppService.SendEDalerUri(Uri));
				if (succeeded)
				{
					await this.NavigationService.GoBackAsync();
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["PaymentSuccess"]);
				}
				else
				{
					this.NotPaid = true;
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToProcessEDalerUri"]);
					this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand, this.SendPaymentCommand);
				}
			}
			catch (Exception ex)
			{
				this.NotPaid = true;
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
				this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand, this.SendPaymentCommand);
			}
		}

		private async Task GenerateQrCode()
		{
			if (!this.NotPaid)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PaymentAlreadySent"]);
				return;
			}

			if (!await App.VerifyPin())
				return;

			try
			{
				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					LegalIdentity LegalIdentity = await this.XmppService.GetLegalIdentity(this.To);
					Uri = await this.XmppService.CreateFullEDalerPaymentUri(LegalIdentity, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}
				else
				{
					Uri = await this.XmppService.CreateFullEDalerPaymentUri(this.To, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				if (this.IsAppearing)
				{
					this.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						this.QrCodeWidth = 300;
						this.QrCodeHeight = 300;
						this.GenerateQrCode(Uri);

						this.EvaluateCommands(this.ShareCommand);

						if (this.shareQrCode is not null)
							await this.shareQrCode.ShowQrCode();
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private bool CanPayOnline() => this.AmountOk && this.AmountExtraOk && !this.HasQrCode && this.IsConnected && this.NotPaid; // TODO: Add To field OK
		private bool CanGenerateQrCode() => this.AmountOk && this.AmountExtraOk && !this.HasQrCode && this.NotPaid; // TODO: Add To field OK
		private bool CanShare() => this.HasQrCode;

		private async Task Share()
		{
			try
			{
				IShareContent shareContent = DependencyService.Get<IShareContent>();
				string Message = this.Message;

				if (string.IsNullOrEmpty(Message))
					Message = this.AmountAndCurrency;

				shareContent.ShareImage(this.QrCodeBin, string.Format(Message, this.Amount, this.Currency),
					LocalizationResourceManager.Current["Share"], "RequestPayment.png");
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Submit()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				(bool succeeded, Transaction Transaction) = await this.NetworkService.TryRequest(() => this.XmppService.SendEDalerUri(this.Uri));
				if (succeeded)
				{
					await this.NavigationService.GoBackAsync();
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["PaymentSuccess"]);
				}
				else
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToProcessEDalerUri"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ShowCode()
		{
			if (!await App.VerifyPin())
				return;

			try
			{
				if (this.IsAppearing)
				{
					this.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						this.QrCodeWidth = 300;
						this.QrCodeHeight = 300;
						this.GenerateQrCode(this.Uri);

						this.EvaluateCommands(this.ShareCommand);

						if (this.shareQrCode is not null)
							await this.shareQrCode.ShowQrCode();
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private bool CanSendPayment()
		{
			return this.uriToSend is not null && this.AmountOk && this.AmountExtraOk && this.NotPaid;
		}

		private async Task SendPayment()
		{
			if (!this.NotPaid)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PaymentAlreadySent"]);
				return;
			}

			if (!await App.VerifyPin())
				return;

			try
			{
				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					LegalIdentity LegalIdentity = await this.XmppService.GetLegalIdentity(this.To);
					Uri = await this.XmppService.CreateFullEDalerPaymentUri(LegalIdentity, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}
				else
				{
					Uri = await this.XmppService.CreateFullEDalerPaymentUri(this.To, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				this.uriToSend?.TrySetResult(Uri);
				await this.NavigationService.GoBackAsync();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Opens the calculator for calculating the value of a numerical property.
		/// </summary>
		/// <param name="Parameter">Property to calculate</param>
		public async Task OpenCalculator(object Parameter)
		{
			try
			{
				switch (Parameter?.ToString())
				{
					case "AmountText":
						await this.NavigationService.GoToAsync(nameof(CalculatorPage), new CalculatorNavigationArgs(this, AmountTextProperty));
						break;

					case "AmountExtraText":
						await this.NavigationService.GoToAsync(nameof(CalculatorPage), new CalculatorNavigationArgs(this, AmountExtraTextProperty));
						break;
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(LocalizationResourceManager.Current["Payment"]);

		#endregion

	}
}
