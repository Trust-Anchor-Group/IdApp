using System;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using IdApp.Navigation.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels.Wallet
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of an eDaler URI.
	/// </summary>
	public class EDalerUriViewModel : NeuronViewModel
	{
		private readonly ITagProfile tagProfile;
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INetworkService networkService;
		private readonly IShareQrCode shareQrCode;

		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public EDalerUriViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			INetworkService networkService,
			ILogService logService,
			IShareQrCode ShareQrCode)
		: base(neuronService, uiDispatcher)
		{
			this.tagProfile = tagProfile;
			this.logService = logService;
			this.navigationService = navigationService;
			this.networkService = networkService;
			this.shareQrCode = ShareQrCode;

			this.AcceptCommand = new Command(async _ => await Accept(), _ => IsConnected);
			this.GenerateQrCodeCommand = new Command(async _ => await this.GenerateQrCode(), _ => this.CanGenerateQrCode());
			this.PayOnlineCommand = new Command(async _ => await this.PayOnline(), _ => this.CanPayOnline());
			this.ShareCommand = new Command(async _ => await this.Share(), _ => this.CanShare());
			this.SubmitCommand = new Command(async _ => await this.Submit(), _ => this.IsConnected);
			this.ShowCodeCommand = new Command(async _ => await this.ShowCode());

			this.FromClickCommand = new Command(async x => await this.FromLabelClicked());
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out EDalerUriNavigationArgs args))
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
				this.HasQrCode = false;

				this.AmountText = this.Amount <= 0 ? string.Empty : this.Amount.ToString();
				this.AmountOk = CommonTypes.TryParse(this.AmountText, out decimal d) && d > 0;
				this.AmountPreset = !string.IsNullOrEmpty(this.AmountText) && this.AmountOk;
				this.AmountAndCurrency = this.AmountText + " " + this.Currency;

				this.AmountExtraText = this.AmountExtra.HasValue ? this.AmountExtra.Value.ToString() : string.Empty;
				this.AmountExtraOk = !this.AmountExtra.HasValue || this.AmountExtra.Value >= 0;
				this.AmountExtraPreset = this.AmountExtra.HasValue;
				this.AmountExtraAndCurrency = this.AmountExtraText + " " + this.Currency;

				StringBuilder Url = new StringBuilder();

				Url.Append("https://");
				Url.Append(this.From);
				Url.Append("/Images/eDalerFront200.png");

				this.EDalerGlyph = Url.ToString();

				if (!(args.Uri.EncryptedMessage is null))
				{
					if (args.Uri.EncryptionPublicKey is null)
						this.Message = Encoding.UTF8.GetString(args.Uri.EncryptedMessage);
					else
					{
						this.Message = await this.NeuronService.Wallet.TryDecryptMessage(args.Uri.EncryptedMessage,
							args.Uri.EncryptionPublicKey, args.Uri.Id, args.Uri.From);
					}

					this.HasMessage = !string.IsNullOrEmpty(this.Message);
				}
			
				this.MessagePreset = !string.IsNullOrEmpty(this.Message);
				this.EncryptMessage = args.Uri.ToType == EntityType.LegalId;
			}

			AssignProperties();
			EvaluateAllCommands();

			this.tagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.tagProfile.Changed -= TagProfile_Changed;
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.AcceptCommand, this.PayOnlineCommand, this.GenerateQrCodeCommand, this.ShareCommand,
				this.SubmitCommand, this.ShareCommand);
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
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create("Uri", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// edaler URI to process
		/// </summary>
		public string Uri
		{
			get { return (string)GetValue(UriProperty); }
			set { SetValue(UriProperty, value); }
		}

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
		/// See <see cref="AmountOk"/>
		/// </summary>
		public static readonly BindableProperty AmountOkProperty =
			BindableProperty.Create("AmountOk", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Amount"/> is OK.
		/// </summary>
		public bool AmountOk
		{
			get { return (bool)GetValue(AmountOkProperty); }
			set { SetValue(AmountOkProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountColor"/>
		/// </summary>
		public static readonly BindableProperty AmountColorProperty =
			BindableProperty.Create("AmountColor", typeof(Color), typeof(EDalerUriViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="Amount"/> field.
		/// </summary>
		public Color AmountColor
		{
			get { return (Color)GetValue(AmountColorProperty); }
			set { SetValue(AmountColorProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountText"/>
		/// </summary>
		public static readonly BindableProperty AmountTextProperty =
			BindableProperty.Create("AmountText", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="Amount"/> as text.
		/// </summary>
		public string AmountText
		{
			get { return (string)GetValue(AmountTextProperty); }
			set
			{
				SetValue(AmountTextProperty, value);

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

				this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand);
			}
		}

		/// <summary>
		/// See <see cref="AmountAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty AmountAndCurrencyProperty =
			BindableProperty.Create("AmountAndCurrency", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="AmountText"/> and <see cref="Currency"/>.
		/// </summary>
		public string AmountAndCurrency
		{
			get { return (string)GetValue(AmountAndCurrencyProperty); }
			set { SetValue(AmountAndCurrencyProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountPreset"/>
		/// </summary>
		public static readonly BindableProperty AmountPresetProperty =
			BindableProperty.Create("AmountPreset", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Amount"/> is preset.
		/// </summary>
		public bool AmountPreset
		{
			get { return (bool)GetValue(AmountPresetProperty); }
			set { SetValue(AmountPresetProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountExtra"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraProperty =
			BindableProperty.Create("AmountExtra", typeof(decimal?), typeof(EDalerUriViewModel), default(decimal?));

		/// <summary>
		/// AmountExtra of eDaler to process
		/// </summary>
		public decimal? AmountExtra
		{
			get { return (decimal?)GetValue(AmountExtraProperty); }
			set { SetValue(AmountExtraProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountExtraOk"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraOkProperty =
			BindableProperty.Create("AmountExtraOk", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="AmountExtra"/> is OK.
		/// </summary>
		public bool AmountExtraOk
		{
			get { return (bool)GetValue(AmountExtraOkProperty); }
			set { SetValue(AmountExtraOkProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountExtraColor"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraColorProperty =
			BindableProperty.Create("AmountExtraColor", typeof(Color), typeof(EDalerUriViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="AmountExtra"/> field.
		/// </summary>
		public Color AmountExtraColor
		{
			get { return (Color)GetValue(AmountExtraColorProperty); }
			set { SetValue(AmountExtraColorProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountExtraText"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraTextProperty =
			BindableProperty.Create("AmountExtraText", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="AmountExtra"/> as text.
		/// </summary>
		public string AmountExtraText
		{
			get { return (string)GetValue(AmountExtraTextProperty); }
			set
			{
				SetValue(AmountExtraTextProperty, value);

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

				this.EvaluateCommands(this.PayOnlineCommand, this.GenerateQrCodeCommand);
			}
		}

		/// <summary>
		/// See <see cref="AmountExtraAndCurrency"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraAndCurrencyProperty =
			BindableProperty.Create("AmountExtraAndCurrency", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// <see cref="AmountExtraText"/> and <see cref="Currency"/>.
		/// </summary>
		public string AmountExtraAndCurrency
		{
			get { return (string)GetValue(AmountExtraAndCurrencyProperty); }
			set { SetValue(AmountExtraAndCurrencyProperty, value); }
		}

		/// <summary>
		/// See <see cref="AmountExtraPreset"/>
		/// </summary>
		public static readonly BindableProperty AmountExtraPresetProperty =
			BindableProperty.Create("AmountExtraPreset", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="AmountExtra"/> is preset.
		/// </summary>
		public bool AmountExtraPreset
		{
			get { return (bool)GetValue(AmountExtraPresetProperty); }
			set { SetValue(AmountExtraPresetProperty, value); }
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
		/// See <see cref="Created"/>
		/// </summary>
		public static readonly BindableProperty CreatedProperty =
			BindableProperty.Create("Created", typeof(DateTime), typeof(EDalerUriViewModel), default(DateTime));

		/// <summary>
		/// When code was created.
		/// </summary>
		public DateTime Created
		{
			get { return (DateTime)GetValue(CreatedProperty); }
			set { SetValue(CreatedProperty, value); }
		}

		/// <summary>
		/// See <see cref="Expires"/>
		/// </summary>
		public static readonly BindableProperty ExpiresProperty =
			BindableProperty.Create("Expires", typeof(DateTime), typeof(EDalerUriViewModel), default(DateTime));

		/// <summary>
		/// When code expires
		/// </summary>
		public DateTime Expires
		{
			get { return (DateTime)GetValue(ExpiresProperty); }
			set { SetValue(ExpiresProperty, value); }
		}

		/// <summary>
		/// See <see cref="ExpiresStr"/>
		/// </summary>
		public static readonly BindableProperty ExpiresStrProperty =
			BindableProperty.Create("ExpiresStr", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// When code expires
		/// </summary>
		public string ExpiresStr
		{
			get { return (string)GetValue(ExpiresStrProperty); }
			set { SetValue(ExpiresStrProperty, value); }
		}

		/// <summary>
		/// See <see cref="Id"/>
		/// </summary>
		public static readonly BindableProperty IdProperty =
			BindableProperty.Create("Id", typeof(Guid), typeof(EDalerUriViewModel), default(Guid));

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		public Guid Id
		{
			get { return (Guid)GetValue(IdProperty); }
			set { SetValue(IdProperty, value); }
		}

		/// <summary>
		/// See <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create("From", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// From who eDaler is to be transferred
		/// </summary>
		public string From
		{
			get { return (string)GetValue(FromProperty); }
			set { SetValue(FromProperty, value); }
		}

		/// <summary>
		/// See <see cref="FromType"/>
		/// </summary>
		public static readonly BindableProperty FromTypeProperty =
			BindableProperty.Create("FromType", typeof(EntityType), typeof(EDalerUriViewModel), default(EntityType));

		/// <summary>
		/// Type of identity specified in <see cref="From"/>
		/// </summary>
		public EntityType FromType
		{
			get { return (EntityType)GetValue(FromTypeProperty); }
			set { SetValue(FromTypeProperty, value); }
		}

		/// <summary>
		/// See <see cref="To"/>
		/// </summary>
		public static readonly BindableProperty ToProperty =
			BindableProperty.Create("To", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// To whom eDaler is to be transferred
		/// </summary>
		public string To
		{
			get { return (string)GetValue(ToProperty); }
			set { SetValue(ToProperty, value); }
		}

		/// <summary>
		/// See <see cref="ToPreset"/>
		/// </summary>
		public static readonly BindableProperty ToPresetProperty =
			BindableProperty.Create("ToPreset", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="To"/> is preset
		/// </summary>
		public bool ToPreset
		{
			get { return (bool)GetValue(ToPresetProperty); }
			set { SetValue(ToPresetProperty, value); }
		}

		/// <summary>
		/// See <see cref="ToType"/>
		/// </summary>
		public static readonly BindableProperty ToTypeProperty =
			BindableProperty.Create("ToType", typeof(EntityType), typeof(EDalerUriViewModel), default(EntityType));

		/// <summary>
		/// Type of identity specified in <see cref="To"/>
		/// </summary>
		public EntityType ToType
		{
			get { return (EntityType)GetValue(ToTypeProperty); }
			set { SetValue(ToTypeProperty, value); }
		}

		/// <summary>
		/// See <see cref="Complete"/>
		/// </summary>
		public static readonly BindableProperty CompleteProperty =
			BindableProperty.Create("Complete", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If the URI is complete or not.
		/// </summary>
		public bool Complete
		{
			get { return (bool)GetValue(CompleteProperty); }
			set { SetValue(CompleteProperty, value); }
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
		/// See <see cref="EncryptMessage"/>
		/// </summary>
		public static readonly BindableProperty EncryptMessageProperty =
			BindableProperty.Create("EncryptMessage", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		public bool EncryptMessage
		{
			get { return (bool)GetValue(EncryptMessageProperty); }
			set { SetValue(EncryptMessageProperty, value); }
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

		/// <summary>
		/// See <see cref="MessagePreset"/>
		/// </summary>
		public static readonly BindableProperty MessagePresetProperty =
			BindableProperty.Create("MessagePreset", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// If <see cref="Message"/> is preset.
		/// </summary>
		public bool MessagePreset
		{
			get { return (bool)GetValue(MessagePresetProperty); }
			set { SetValue(MessagePresetProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCode"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create("QrCode", typeof(ImageSource), typeof(EDalerUriViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				EDalerUriViewModel viewModel = (EDalerUriViewModel)b;
				viewModel.HasQrCode = !(newValue is null);
			});


		/// <summary>
		/// Gets or sets the current user's identity as a QR code image.
		/// </summary>
		public ImageSource QrCode
		{
			get { return (ImageSource)GetValue(QrCodeProperty); }
			set { SetValue(QrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasQrCode"/>
		/// </summary>
		public static readonly BindableProperty HasQrCodeProperty =
			BindableProperty.Create("HasQrCode", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// Gets or sets if a <see cref="QrCode"/> exists for the current user.
		/// </summary>
		public bool HasQrCode
		{
			get { return (bool)GetValue(HasQrCodeProperty); }
			set { SetValue(HasQrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodePng"/>
		/// </summary>
		public static readonly BindableProperty QrCodePngProperty =
			BindableProperty.Create("QrCodePng", typeof(byte[]), typeof(EDalerUriViewModel), default(byte[]));

		/// <summary>
		/// Gets or sets if a <see cref="QrCode"/> exists for the current user.
		/// </summary>
		public byte[] QrCodePng
		{
			get { return (byte[])GetValue(QrCodePngProperty); }
			set { SetValue(QrCodePngProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create("QrCodeWidth", typeof(int), typeof(EDalerUriViewModel), default(int));

		/// <summary>
		/// Gets or sets if a <see cref="QrCode"/> exists for the current user.
		/// </summary>
		public int QrCodeWidth
		{
			get { return (int)GetValue(QrCodeWidthProperty); }
			set { SetValue(QrCodeWidthProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeHeight"/>
		/// </summary>
		public static readonly BindableProperty QrCodeHeightProperty =
			BindableProperty.Create("QrCodeHeight", typeof(int), typeof(EDalerUriViewModel), default(int));

		/// <summary>
		/// Gets or sets if a <see cref="QrCode"/> exists for the current user.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)GetValue(QrCodeHeightProperty); }
			set { SetValue(QrCodeHeightProperty, value); }
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
			get { return this.NeuronService.State == XmppState.Connected; }
		}

		/// <summary>
		/// See <see cref="CanAccept"/>
		/// </summary>
		public static readonly BindableProperty CanAcceptProperty =
			BindableProperty.Create("CanAccept", typeof(bool), typeof(EDalerUriViewModel), default(bool));

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
		/// If PIN should be used.
		/// </summary>
		public bool UsePin => this.tagProfile?.UsePin ?? false;

		/// <summary>
		/// See <see cref="Pin"/>
		/// </summary>
		public static readonly BindableProperty PinProperty =
			BindableProperty.Create("Pin", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Gets or sets the PIN code for the identity.
		/// </summary>
		public string Pin
		{
			get { return (string)GetValue(PinProperty); }
			set { SetValue(PinProperty, value); }
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

		#endregion

		/// <summary>
		/// Command to bind to for detecting when a from label has been clicked on.
		/// </summary>
		public ICommand FromClickCommand { get; }

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
				await UiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
			}
			catch (Exception ex)
			{
				logService.LogException(ex);
				await UiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task Accept()
		{
			try
			{
				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

				(bool succeeded, Transaction Transaction) = await this.networkService.TryRequest(() => this.NeuronService.Wallet.SendUri(this.Uri));
				if (succeeded)
				{
					await this.navigationService.GoBackAsync();
					await this.UiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.TransactionAccepted);
				}
				else
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToProcessEDalerUri);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task PayOnline()
		{
			try
			{
				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					LegalIdentity LegalIdentity = await this.NeuronService.Contracts.GetLegalIdentity(this.To);
					Uri = await this.NeuronService.Wallet.CreateFullPaymentUri(LegalIdentity, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}
				else
				{
					Uri = await this.NeuronService.Wallet.CreateFullPaymentUri(this.To, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				(bool succeeded, Transaction Transaction) = await this.networkService.TryRequest(() => this.NeuronService.Wallet.SendUri(Uri));
				if (succeeded)
				{
					await this.navigationService.GoBackAsync();
					await this.UiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.PaymentSuccess);
				}
				else
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToProcessEDalerUri);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task GenerateQrCode()
		{
			if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
				return;
			}

			try
			{
				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					LegalIdentity LegalIdentity = await this.NeuronService.Contracts.GetLegalIdentity(this.To);
					Uri = await this.NeuronService.Wallet.CreateFullPaymentUri(LegalIdentity, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}
				else
				{
					Uri = await this.NeuronService.Wallet.CreateFullPaymentUri(this.To, this.Amount, this.AmountExtra,
						this.Currency, 3, this.Message);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				byte[] Bin = QrCodeImageGenerator.GeneratePng(Uri, 300, 300);
				this.QrCodePng = Bin;

				if (this.IsBound)
				{
					this.UiDispatcher.BeginInvokeOnMainThread(async () =>
					{
						this.QrCode = ImageSource.FromStream(() => new MemoryStream(Bin));
						this.QrCodeWidth = 300;
						this.QrCodeHeight = 300;
						this.HasQrCode = true;

						this.EvaluateCommands(this.ShareCommand);

						if (!(this.shareQrCode is null))
							await this.shareQrCode.ShowQrCode();
					});
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

		private bool CanPayOnline() => this.AmountOk && this.AmountExtraOk && !this.HasQrCode && this.IsConnected; // TODO: Add To field OK
		private bool CanGenerateQrCode() => this.AmountOk && this.AmountExtraOk && !this.HasQrCode;	// TODO: Add To field OK
		private bool CanShare() => this.HasQrCode;

		private async Task Share()
		{
			try
			{
				IShareContent shareContent = DependencyService.Get<IShareContent>();

				shareContent.ShareImage(this.QrCodePng, string.Format(AppResources.RequestPaymentMessage, this.Amount, this.Currency),
					AppResources.Share, "RequestPayment.png");
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task Submit()
		{
			try
			{
				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

				(bool succeeded, Transaction Transaction) = await this.networkService.TryRequest(() => this.NeuronService.Wallet.SendUri(this.Uri));
				if (succeeded)
				{
					await this.navigationService.GoBackAsync();
					await this.UiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.PaymentSuccess);
				}
				else
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToProcessEDalerUri);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task ShowCode()
		{
			if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
				return;
			}

			try
			{
				byte[] Bin = QrCodeImageGenerator.GeneratePng(this.Uri, 300, 300);
				this.QrCodePng = Bin;

				if (this.IsBound)
				{
					this.UiDispatcher.BeginInvokeOnMainThread(async () =>
					{
						this.QrCode = ImageSource.FromStream(() => new MemoryStream(Bin));
						this.QrCodeWidth = 300;
						this.QrCodeHeight = 300;
						this.HasQrCode = true;

						this.EvaluateCommands(this.ShareCommand);

						if (!(this.shareQrCode is null))
							await this.shareQrCode.ShowQrCode();
					});
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}

	}
}