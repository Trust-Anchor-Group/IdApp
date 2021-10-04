using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Waher.Content;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.RequestPayment
{
	/// <summary>
	/// The view model to bind to for requesting a payment.
	/// </summary>
	public class RequestPaymentViewModel : NeuronViewModel
	{
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly RequestPaymentPage page;

		/// <summary>
		/// Creates an instance of the <see cref="RequestPaymentViewModel"/> class.
		/// </summary>
		public RequestPaymentViewModel(
			ITagProfile tagProfile,
			IUiSerializer uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ILogService logService,
			RequestPaymentPage page)
		: base(neuronService, uiDispatcher, tagProfile)
		{
			this.logService = logService;
			this.navigationService = navigationService;
			this.page = page;

			this.GenerateQrCodeCommand = new Command(async _ => await this.GenerateQrCode(), _ => this.CanGenerateQrCode());
			this.ShareCommand = new Command(async _ => await this.Share(), _ => this.CanShare());
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out EDalerBalanceNavigationArgs args))
				this.Currency = args.Balance.Currency;

			this.Amount = 0;
			this.AmountText = string.Empty;
			this.AmountOk = false;

			this.AmountExtra = 0;
			this.AmountExtraText = string.Empty;
			this.AmountExtraOk = false;
			
			this.HasQrCode = false;

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
			this.EvaluateCommands(this.GenerateQrCodeCommand, this.ShareCommand);
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

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create("Amount", typeof(decimal), typeof(RequestPaymentViewModel), default(decimal));

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
			BindableProperty.Create("AmountOk", typeof(bool), typeof(RequestPaymentViewModel), default(bool));

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
			BindableProperty.Create("AmountColor", typeof(Color), typeof(RequestPaymentViewModel), default(Color));

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
			BindableProperty.Create("AmountText", typeof(string), typeof(RequestPaymentViewModel), default(string));

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

				this.EvaluateCommands(this.GenerateQrCodeCommand);
			}
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

				this.EvaluateCommands(this.GenerateQrCodeCommand);
			}
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create("Currency", typeof(string), typeof(RequestPaymentViewModel), default(string));

		/// <summary>
		/// Currency of <see cref="Amount"/>.
		/// </summary>
		public string Currency
		{
			get { return (string)GetValue(CurrencyProperty); }
			set { SetValue(CurrencyProperty, value); }
		}

		/// <summary>
		/// See <see cref="Message"/>
		/// </summary>
		public static readonly BindableProperty MessageProperty =
			BindableProperty.Create("Message", typeof(string), typeof(RequestPaymentViewModel), default(string));

		/// <summary>
		/// Message to embed in payment.
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
			BindableProperty.Create("EncryptMessage", typeof(bool), typeof(RequestPaymentViewModel), default(bool));

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		public bool EncryptMessage
		{
			get { return (bool)GetValue(EncryptMessageProperty); }
			set { SetValue(EncryptMessageProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCode"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create("QrCode", typeof(ImageSource), typeof(RequestPaymentViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				RequestPaymentViewModel viewModel = (RequestPaymentViewModel)b;
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
			BindableProperty.Create("HasQrCode", typeof(bool), typeof(RequestPaymentViewModel), default(bool));

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
			BindableProperty.Create("QrCodePng", typeof(byte[]), typeof(RequestPaymentViewModel), default(byte[]));

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
			BindableProperty.Create("QrCodeWidth", typeof(int), typeof(RequestPaymentViewModel), default(int));

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
			BindableProperty.Create("QrCodeHeight", typeof(int), typeof(RequestPaymentViewModel), default(int));

		/// <summary>
		/// Gets or sets if a <see cref="QrCode"/> exists for the current user.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)GetValue(QrCodeHeightProperty); }
			set { SetValue(QrCodeHeightProperty, value); }
		}

		/// <summary>
		/// The command to bind to for generating a QR code
		/// </summary>
		public ICommand GenerateQrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sharing the QR code
		/// </summary>
		public ICommand ShareCommand { get; }

		#endregion

		private Task GenerateQrCode()
		{
			string Uri;

			if (this.EncryptMessage)
			{
				Uri = this.NeuronService.Wallet.CreateIncompletePayMeUri(this.TagProfile.LegalIdentity, this.Amount, this.AmountExtra,
					this.Currency, this.Message);
			}
			else
			{
				Uri = this.NeuronService.Wallet.CreateIncompletePayMeUri(this.NeuronService.Xmpp.BareJID, this.Amount, this.AmountExtra,
					this.Currency, this.Message);
			}

			byte[] Bin = IdApp.QrCode.GeneratePng(Uri, 300, 300);
			this.QrCodePng = Bin;

			if (this.IsBound)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					this.QrCode = ImageSource.FromStream(() => new MemoryStream(Bin));
					this.QrCodeWidth = 300;
					this.QrCodeHeight = 300;
					this.HasQrCode = true;

					this.EvaluateCommands(this.ShareCommand);

					await this.page.ShowQrCode();
				});
			}

			return Task.CompletedTask;
		}

		private bool CanGenerateQrCode() => this.AmountOk;
		private bool CanShare() => this.HasQrCode;

		private async Task Share()
		{
			try
			{
				IShareContent shareContent = DependencyService.Get<IShareContent>();
				string Message = this.Message;

				if (string.IsNullOrEmpty(Message))
					Message = AppResources.RequestPaymentMessage;

				shareContent.ShareImage(this.QrCodePng, string.Format(Message, this.Amount, this.Currency), 
					AppResources.Share, "RequestPayment.png");
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

	}
}