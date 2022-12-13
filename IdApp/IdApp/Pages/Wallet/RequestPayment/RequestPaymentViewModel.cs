using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.DeviceSpecific;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Main.Calculator;
using Waher.Content;
using Waher.Networking.XMPP;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.RequestPayment
{
	/// <summary>
	/// The view model to bind to for requesting a payment.
	/// </summary>
	public class RequestPaymentViewModel : QrXmppViewModel
	{
		private readonly RequestPaymentPage page;

		/// <summary>
		/// Creates an instance of the <see cref="RequestPaymentViewModel"/> class.
		/// </summary>
		public RequestPaymentViewModel(RequestPaymentPage page)
		: base()
		{
			this.page = page;

			this.GenerateQrCodeCommand = new Command(async _ => await this.GenerateQrCode(), _ => this.CanGenerateQrCode());
			this.ShareContactCommand = new Command(async _ => await this.ShareContact(), _ => this.CanShare());
			this.ShareExternalCommand = new Command(async _ => await this.ShareExternal(), _ => this.CanShare());
			this.OpenCalculatorCommand = new Command(async P => await this.OpenCalculator(P));
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			bool SkipInitialization = false;

			if (this.NavigationService.TryPopArgs(out EDalerBalanceNavigationArgs args))
			{
				SkipInitialization = args.ViewInitialized;
				if (!SkipInitialization)
				{
					this.Currency = args.Balance.Currency;
					args.ViewInitialized = true;
				}
			}

			if (!SkipInitialization)
			{
				this.Amount = 0;
				this.AmountText = string.Empty;
				this.AmountOk = false;

				this.AmountExtra = 0;
				this.AmountExtraText = string.Empty;
				this.AmountExtraOk = false;
			}

			this.RemoveQrCode();

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
			this.EvaluateCommands(this.GenerateQrCodeCommand, this.ShareContactCommand, this.ShareExternalCommand,
				this.OpenCalculatorCommand);
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object Sender, XmppState NewState)
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
			BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(RequestPaymentViewModel), default(decimal));

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
			BindableProperty.Create(nameof(AmountOk), typeof(bool), typeof(RequestPaymentViewModel), default(bool));

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
			BindableProperty.Create(nameof(AmountColor), typeof(Color), typeof(RequestPaymentViewModel), default(Color));

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
			BindableProperty.Create(nameof(AmountText), typeof(string), typeof(RequestPaymentViewModel), default(string));

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

				this.EvaluateCommands(this.GenerateQrCodeCommand);
			}
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

				this.EvaluateCommands(this.GenerateQrCodeCommand);
			}
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create(nameof(Currency), typeof(string), typeof(RequestPaymentViewModel), default(string));

		/// <summary>
		/// Currency of <see cref="Amount"/>.
		/// </summary>
		public string Currency
		{
			get => (string)this.GetValue(CurrencyProperty);
			set => this.SetValue(CurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="Message"/>
		/// </summary>
		public static readonly BindableProperty MessageProperty =
			BindableProperty.Create(nameof(Message), typeof(string), typeof(RequestPaymentViewModel), default(string));

		/// <summary>
		/// Message to embed in payment.
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
			BindableProperty.Create(nameof(EncryptMessage), typeof(bool), typeof(RequestPaymentViewModel), default(bool));

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		public bool EncryptMessage
		{
			get => (bool)this.GetValue(EncryptMessageProperty);
			set => this.SetValue(EncryptMessageProperty, value);
		}

		/// <summary>
		/// The command to bind to for generating a QR code
		/// </summary>
		public ICommand GenerateQrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sharing the QR code with a contact
		/// </summary>
		public ICommand ShareContactCommand { get; }

		/// <summary>
		/// The command to bind to for sharing the QR code with external applications
		/// </summary>
		public ICommand ShareExternalCommand { get; }

		/// <summary>
		/// The command to bind to open the calculator.
		/// </summary>
		public ICommand OpenCalculatorCommand { get; }

		#endregion

		private Task GenerateQrCode()
		{
			string Uri;

			if (this.EncryptMessage)
			{
				Uri = this.XmppService.Wallet.CreateIncompletePayMeUri(this.TagProfile.LegalIdentity, this.Amount, this.AmountExtra,
					this.Currency, this.Message);
			}
			else
			{
				Uri = this.XmppService.Wallet.CreateIncompletePayMeUri(this.XmppService.Xmpp.BareJID, this.Amount, this.AmountExtra,
					this.Currency, this.Message);
			}

			if (this.IsAppearing)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					this.QrCodeWidth = 300;
					this.QrCodeHeight = 300;
					this.GenerateQrCode(Uri);

					this.EvaluateCommands(this.ShareContactCommand, this.ShareExternalCommand);

					await this.page.ShowQrCode();
				});
			}

			return Task.CompletedTask;
		}

		private bool CanGenerateQrCode() => this.AmountOk;
		private bool CanShare() => this.HasQrCode;

		private async Task ShareContact()
		{
			try
			{
				TaskCompletionSource<ContactInfoModel> Selected = new();
				ContactListNavigationArgs Args = new(LocalizationResourceManager.Current["SelectFromWhomToRequestPayment"], Selected)
				{
					CanScanQrCode = true,
					CancelReturnCounter = true
				};

				await this.NavigationService.GoToAsync(nameof(MyContactsPage), Args);

				ContactInfoModel Contact = await Selected.Task;
				if (Contact is null)
					return;

				if (string.IsNullOrEmpty(Contact.BareJid))
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkAddressOfContactUnknown"]);
					return;
				}

				StringBuilder Markdown = new();

				Markdown.Append("![");
				Markdown.Append(LocalizationResourceManager.Current["RequestPayment"]);
				Markdown.Append("](");
				Markdown.Append(this.QrCodeUri);
				Markdown.Append(')');

				await ChatViewModel.ExecuteSendMessage(string.Empty, Markdown.ToString(), Contact.BareJid, this);

				await Task.Delay(100);  // Otherwise, page doesn't show properly. (Underlying timing issue. TODO: Find better solution.)

				await this.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(Contact.Contact)
				{
					UniqueId = Contact.BareJid
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ShareExternal()
		{
			try
			{
				IShareContent shareContent = DependencyService.Get<IShareContent>();
				string Message = this.Message;

				if (string.IsNullOrEmpty(Message))
					Message = LocalizationResourceManager.Current["RequestPaymentMessage"];

				shareContent.ShareImage(this.QrCodeBin, string.Format(Message, this.Amount, this.Currency), 
					LocalizationResourceManager.Current["Share"], "RequestPayment.png");
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
		public override Task<string> Title => Task.FromResult<string>(LocalizationResourceManager.Current["RequestPayment"]);

		#endregion


	}
}
