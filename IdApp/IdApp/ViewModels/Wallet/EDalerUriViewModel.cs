using System;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EDaler;
using IdApp.Navigation.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
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

		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public EDalerUriViewModel(
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

			this.AcceptCommand = new Command(async _ => await Accept(), _ => IsConnected);

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
				this.Currency = args.Uri.Currency;
				this.Created = args.Uri.Created;
				this.Expires = args.Uri.Expires;
				this.From = args.Uri.From;
				this.FromType = args.Uri.FromType;
				this.To = args.Uri.To;
				this.ToType = args.Uri.ToType;
				this.Complete = args.Uri.Complete;

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
							args.Uri.EncryptionPublicKey, args.Uri.From);
					}

					this.HasMessage = !string.IsNullOrEmpty(this.Message);
				}
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
			BindableProperty.Create("HasMessage", typeof(bool), typeof(EDalerUriViewModel), default(bool));

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
		/// See <see cref="CanAccept"/>
		/// </summary>
		public static readonly BindableProperty CanAcceptProperty =
			BindableProperty.Create("CanAccept", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can accept the URI or not.
		/// </summary>
		public bool CanAccept
		{
			get { return this.NeuronService.State == XmppState.Connected; }
		}

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
					await this.navigationService.GoBackAsync();
				else
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToProcessEDalerUri);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}
	}
}