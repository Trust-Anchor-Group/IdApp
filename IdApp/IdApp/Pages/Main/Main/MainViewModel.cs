using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contacts;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;
using IdApp.Services.AttachmentCache;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.ThingRegistries;
using IdApp.Services.Wallet;
using IdApp.Services.UI;
using IdApp.Services.UI.Photos;
using IdApp.Services.Data.Countries;

namespace IdApp.Pages.Main.Main
{
	/// <summary>
	/// The view model to bind to for the main page of the application. Holds basic user profile state
	/// as well as connection state.
	/// </summary>
	public class MainViewModel : NeuronViewModel
	{
		private readonly ILogService logService;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly IThingRegistryOrchestratorService thingRegistryOrchestratorService;
		private readonly IEDalerOrchestratorService eDalerOrchestratorService;
		private readonly PhotosLoader photosLoader;

		/// <summary>
		/// Creates a new instance of the <see cref="MainViewModel"/> class.
		/// </summary>
		public MainViewModel()
			: this(null, null, null, null, null, null, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="MainViewModel"/> class.
		/// For unit tests.
		/// </summary>
		protected internal MainViewModel(
			ILogService logService,
			INeuronService neuronService,
			IUiSerializer uiSerializer,
			ITagProfile tagProfile,
			INavigationService navigationService,
			INetworkService networkService,
			IAttachmentCacheService attachmentCacheService,
			IContractOrchestratorService contractOrchestratorService,
			IThingRegistryOrchestratorService thingThingRegistryOrchestratorService,
			IEDalerOrchestratorService eDalerOrchestratorService)
			: base(neuronService, uiSerializer, tagProfile)
		{
			this.logService = logService ?? App.Instantiate<ILogService>();
			this.navigationService = navigationService ?? App.Instantiate<INavigationService>();
			this.networkService = networkService ?? App.Instantiate<INetworkService>();
			this.contractOrchestratorService = contractOrchestratorService ?? App.Instantiate<IContractOrchestratorService>();
			this.thingRegistryOrchestratorService = thingThingRegistryOrchestratorService ?? App.Instantiate<IThingRegistryOrchestratorService>();
			this.eDalerOrchestratorService = eDalerOrchestratorService ?? App.Instantiate<IEDalerOrchestratorService>();

			this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.NeuronService, this.UiSerializer,
				attachmentCacheService ?? App.Instantiate<IAttachmentCacheService>());

			this.ViewMyContactsCommand = new Command(async () => await ViewMyContacts(), () => this.IsConnected);
			this.ViewMyThingsCommand = new Command(async () => await ViewMyThings(), () => this.IsConnected);
			this.ScanQrCodeCommand = new Command(async () => await ScanQrCode());
			this.ViewSignedContractsCommand = new Command(async () => await ViewSignedContracts(), () => this.IsConnected);
			this.ViewWalletCommand = new Command(async () => await ViewWallet(), () => this.IsConnected);
			this.SharePhotoCommand = new Command(async () => await SharePhoto());
			this.ShareQRCommand = new Command(async () => await ShareQR());
		}

		/// <inheritdoc />
		protected override async Task DoBind()
		{
			await base.DoBind();
			this.AssignProperties();
			this.SetConnectionStateAndText(this.NeuronService.State);
			this.NeuronService.ConnectionStateChanged += Contracts_ConnectionStateChanged;
			this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
		}

		/// <inheritdoc />
		protected override Task DoUnbind()
		{
			this.photosLoader.CancelLoadPhotos();
			this.NeuronService.ConnectionStateChanged -= Contracts_ConnectionStateChanged;
			this.networkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;
			return base.DoUnbind();
		}

		private void AssignProperties()
		{
			if (!(this.TagProfile?.LegalIdentity is null))
			{
				string firstName = this.TagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
				string lastNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.LastName];

				if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
					this.FullName = $"{firstName} {lastNames}";
				else if (!string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastNames))
					this.FullName = firstName;
				else if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
					this.FullName = lastNames;
				else
					this.FullName = string.Empty;

				this.City = this.TagProfile.LegalIdentity[Constants.XmppProperties.City];
				string countryCode = this.TagProfile.LegalIdentity[Constants.XmppProperties.Country];

				if (ISO_3166_1.TryGetCountry(countryCode, out string country))
					this.Country = country;
				else
					this.Country = string.Empty;

				this.QrCodeBin = Services.UI.QR.QrCode.GeneratePng(Constants.UriSchemes.CreateIdUri(this.TagProfile.LegalIdentity.Id), this.QrCodeWidth, this.QrCodeHeight);
				this.QrCodeContentType = "image/png";
				this.QrCode = ImageSource.FromStream(() => new MemoryStream(this.QrCodeBin));

				Attachment firstAttachment = this.TagProfile.LegalIdentity.Attachments?.GetFirstImageAttachment();
				if (!(firstAttachment is null))
				{
					_ = Task.Run(async () =>
					{
						try
						{
							await this.LoadProfilePhoto(firstAttachment);
						}
						catch (Exception ex)
						{
							this.logService.LogException(ex);
							await this.UiSerializer.DisplayAlert(ex);
						}
					});
				}
			}
			else
			{
				this.FullName = string.Empty;
				this.City = string.Empty;
				this.Country = string.Empty;
				this.QrCode = null;
				this.QrCodeBin = null;
				this.QrCodeContentType = string.Empty;
			}
		}

		private async Task LoadProfilePhoto(Attachment firstAttachment)
		{
			try
			{
				bool connected = await this.NeuronService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
				if (!connected)
					return;

				(byte[] Bin, string ContentType, int Rotation) = await this.photosLoader.LoadOnePhoto(firstAttachment, SignWith.LatestApprovedIdOrCurrentKeys);

				this.ImageBin = Bin;
				this.ImageContentType = ContentType;

				if (!(Bin is null))
				{
					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (this.IsBound)
						{
							this.ImageRotation = Rotation;
							this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));
						}
					});
				}
			}
			catch (Exception e)
			{
				this.logService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}
		}

		#region Properties

		/// <summary>
		/// See <see cref="ViewMyContactsCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewMyContactsCommandProperty =
			BindableProperty.Create("ViewMyContactsCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ViewMyContactsCommand
		{
			get { return (ICommand)GetValue(ViewMyContactsCommandProperty); }
			set { SetValue(ViewMyContactsCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="ViewMyThingsCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewMyThingsCommandProperty =
			BindableProperty.Create("ViewMyThingsCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ViewMyThingsCommand
		{
			get { return (ICommand)GetValue(ViewMyThingsCommandProperty); }
			set { SetValue(ViewMyThingsCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="ScanQrCodeCommand"/>
		/// </summary>
		public static readonly BindableProperty ScanQrCodeCommandProperty =
			BindableProperty.Create("ScanQrCodeCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for scanning a QR code.
		/// </summary>
		public ICommand ScanQrCodeCommand
		{
			get { return (ICommand)GetValue(ScanQrCodeCommandProperty); }
			set { SetValue(ScanQrCodeCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="ViewSignedContractsCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewSignedContractsCommandProperty =
			BindableProperty.Create("ViewSignedContractsCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing a user's wallet.
		/// </summary>
		public ICommand ViewSignedContractsCommand
		{
			get { return (ICommand)GetValue(ViewSignedContractsCommandProperty); }
			set { SetValue(ViewSignedContractsCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="ViewWalletCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewWalletCommandProperty =
			BindableProperty.Create("ViewWalletCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing a user's wallet.
		/// </summary>
		public ICommand ViewWalletCommand
		{
			get { return (ICommand)GetValue(ViewWalletCommandProperty); }
			set { SetValue(ViewWalletCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="SharePhotoCommand"/>
		/// </summary>
		public static readonly BindableProperty SharePhotoCommandProperty =
			BindableProperty.Create("SharePhotoCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand SharePhotoCommand
		{
			get { return (ICommand)GetValue(SharePhotoCommandProperty); }
			set { SetValue(SharePhotoCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="ShareQRCommand"/>
		/// </summary>
		public static readonly BindableProperty ShareQRCommandProperty =
			BindableProperty.Create("ShareQRCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ShareQRCommand
		{
			get { return (ICommand)GetValue(ShareQRCommandProperty); }
			set { SetValue(ShareQRCommandProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty HasPhotoProperty =
			BindableProperty.Create("HasPhoto", typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public bool HasPhoto
		{
			get { return (bool)GetValue(HasPhotoProperty); }
			set { SetValue(HasPhotoProperty, value); }
		}

		/// <summary>
		/// See <see cref="Image"/>
		/// </summary>
		public static readonly BindableProperty ImageProperty =
			BindableProperty.Create("Image", typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
				viewModel.HasPhoto = !(newValue is null);
			});

		/// <summary>
		/// Gets or sets the current user's photo.
		/// </summary>
		public ImageSource Image
		{
			get { return (ImageSource)GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}

		/// <summary>
		/// See <see cref="ImageRotation"/>
		/// </summary>
		public static readonly BindableProperty ImageRotationProperty =
			BindableProperty.Create("ImageRotation", typeof(int), typeof(MainViewModel), default(int));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public int ImageRotation
		{
			get { return (int)GetValue(ImageRotationProperty); }
			set { SetValue(ImageRotationProperty, value); }
		}

		/// <summary>
		/// See <see cref="ImageBin"/>
		/// </summary>
		public static readonly BindableProperty ImageBinProperty =
			BindableProperty.Create("ImageBin", typeof(byte[]), typeof(MainViewModel), default(byte[]));

		/// <summary>
		/// Binary encoding of photo
		/// </summary>
		public byte[] ImageBin
		{
			get { return (byte[])GetValue(ImageBinProperty); }
			set { SetValue(ImageBinProperty, value); }
		}

		/// <summary>
		/// See <see cref="ImageContentType"/>
		/// </summary>
		public static readonly BindableProperty ImageContentTypeProperty =
			BindableProperty.Create("ImageContentType", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Content-Type of photo
		/// </summary>
		public string ImageContentType
		{
			get { return (string)GetValue(ImageContentTypeProperty); }
			set { SetValue(ImageContentTypeProperty, value); }
		}

		/// <summary>
		/// See <see cref="FullName"/>
		/// </summary>
		public static readonly BindableProperty FullNameProperty =
			BindableProperty.Create("FullName", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the current user's full name.
		/// </summary>
		public string FullName
		{
			get { return (string)GetValue(FullNameProperty); }
			set { SetValue(FullNameProperty, value); }
		}

		/// <summary>
		/// See <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create("City", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
				viewModel.SetLocation();
			});

		/// <summary>
		/// Gets or sets the current user's city.
		/// </summary>
		public string City
		{
			get { return (string)GetValue(CityProperty); }
			set { SetValue(CityProperty, value); }
		}

		/// <summary>
		/// See <see cref="Country"/>
		/// </summary>
		public static readonly BindableProperty CountryProperty =
			BindableProperty.Create("Country", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
				viewModel.SetLocation();
			});

		/// <summary>
		/// Gets or sets the current user's country.
		/// </summary>
		public string Country
		{
			get { return (string)GetValue(CountryProperty); }
			set { SetValue(CountryProperty, value); }
		}

		/// <summary>
		/// See <see cref="Location"/>
		/// </summary>
		public static readonly BindableProperty LocationProperty =
			BindableProperty.Create("Location", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the current user's location.
		/// </summary>
		public string Location
		{
			get { return (string)GetValue(LocationProperty); }
			set { SetValue(LocationProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCode"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create("QrCode", typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
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
			BindableProperty.Create("HasQrCode", typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets if a <see cref="QrCode"/> exists for the current user.
		/// </summary>
		public bool HasQrCode
		{
			get { return (bool)GetValue(HasQrCodeProperty); }
			set { SetValue(HasQrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create("QrCodeWidth", typeof(int), typeof(MainViewModel), UiConstants.QrCode.DefaultImageWidth);

		/// <summary>
		/// Gets or sets the width, in pixels, of the <see cref="QrCode"/> being generated.
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
			BindableProperty.Create("QrCodeHeight", typeof(int), typeof(MainViewModel), UiConstants.QrCode.DefaultImageHeight);

		/// <summary>
		/// Gets or sets the height, in pixels, of the <see cref="QrCode"/> being generated.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)GetValue(QrCodeHeightProperty); }
			set { SetValue(QrCodeHeightProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeBin"/>
		/// </summary>
		public static readonly BindableProperty QrCodeBinProperty =
			BindableProperty.Create("QrCodeBin", typeof(byte[]), typeof(MainViewModel), default(byte[]));

		/// <summary>
		/// Binary encoding of QR Code
		/// </summary>
		public byte[] QrCodeBin
		{
			get { return (byte[])GetValue(QrCodeBinProperty); }
			set { SetValue(QrCodeBinProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeContentType"/>
		/// </summary>
		public static readonly BindableProperty QrCodeContentTypeProperty =
			BindableProperty.Create("QrCodeContentType", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Content-Type of QR Code
		/// </summary>
		public string QrCodeContentType
		{
			get { return (string)GetValue(QrCodeContentTypeProperty); }
			set { SetValue(QrCodeContentTypeProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsOnline"/>
		/// </summary>
		public static readonly BindableProperty IsOnlineProperty =
			BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the app is currently online, i.e. has network access.
		/// </summary>
		public bool IsOnline
		{
			get { return (bool)GetValue(IsOnlineProperty); }
			set { SetValue(IsOnlineProperty, value); }
		}

		/// <summary>
		/// See <see cref="NetworkStateText"/>
		/// </summary>
		public static readonly BindableProperty NetworkStateTextProperty =
			BindableProperty.Create("NetworkStateText", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the user friendly network state text for display.
		/// </summary>
		public string NetworkStateText
		{
			get { return (string)GetValue(NetworkStateTextProperty); }
			set { SetValue(NetworkStateTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="IdentityStateText"/>
		/// </summary>
		public static readonly BindableProperty IdentityStateTextProperty =
			BindableProperty.Create("IdentityStateText", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the user friendly network state text for display.
		/// </summary>
		public string IdentityStateText
		{
			get { return (string)GetValue(IdentityStateTextProperty); }
			set { SetValue(IdentityStateTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasConnectionErrors"/>
		/// </summary>
		public static readonly BindableProperty HasConnectionErrorsProperty =
			BindableProperty.Create("HasConnectionErrors", typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether there are any connection errors at all for the app.
		/// </summary>
		public bool HasConnectionErrors
		{
			get { return (bool)GetValue(HasConnectionErrorsProperty); }
			set { SetValue(HasConnectionErrorsProperty, value); }
		}

		/// <summary>
		/// See <see cref="ConnectionErrorsText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionErrorsTextProperty =
			BindableProperty.Create("ConnectionErrorsText", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the user friendly connection errors text for display. Can be null.
		/// </summary>
		public string ConnectionErrorsText
		{
			get { return (string)GetValue(ConnectionErrorsTextProperty); }
			set { SetValue(ConnectionErrorsTextProperty, value); }
		}

		#endregion

		private async Task ViewMyContacts()
		{
			await this.navigationService.GoToAsync(nameof(MyContactsPage), 
				new ContactListNavigationArgs(AppResources.ContactsDescription, SelectContactAction.ViewIdentity));
		}

		private async Task ViewMyThings()
		{
			await this.navigationService.GoToAsync(nameof(Things.MyThings.MyThingsPage));
		}

		private async Task ViewSignedContracts()
		{
			await this.navigationService.GoToAsync(nameof(Contracts.SignedContracts.SignedContractsPage));
		}

		private async Task ViewWallet()
		{
			await this.eDalerOrchestratorService.OpenWallet();
		}

		private async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult(this.logService, this.NeuronService, this.navigationService,
				this.UiSerializer, this.contractOrchestratorService, this.thingRegistryOrchestratorService,
				this.eDalerOrchestratorService);
		}

		private void SetLocation()
		{
			if (!string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Country))
			{
				Location = $"{City}, {Country}";
			}
			if (!string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(Country))
			{
				Location = City;
			}
			if (string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Country))
			{
				Location = Country;
			}
		}

		/// <inheritdoc />
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
			});
		}

		private void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(this.NeuronService.State);
			});
		}

		private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() => SetConnectionStateAndText(this.NeuronService.State));
		}

		/// <inheritdoc/>
		protected override void SetConnectionStateAndText(XmppState state)
		{
			try
			{
				// Network
				this.IsOnline = this.networkService?.IsOnline ?? false;
				this.NetworkStateText = this.IsOnline ? AppResources.Online : AppResources.Offline;
				this.IdentityStateText = this.TagProfile?.LegalIdentity?.State.ToDisplayText() ?? string.Empty;

				// Neuron server
				this.IsConnected = state == XmppState.Connected;
				this.ConnectionStateText = state.ToDisplayText();
				this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
				this.StateSummaryText = (this.TagProfile?.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;

				// Any connection errors or general errors that should be displayed?
				string latestError = this.NeuronService?.LatestError ?? string.Empty;
				string latestConnectionError = this.NeuronService?.LatestConnectionError ?? string.Empty;

				if (!string.IsNullOrWhiteSpace(latestError) && !string.IsNullOrWhiteSpace(latestConnectionError))
				{
					if (latestConnectionError != latestError)
						this.ConnectionErrorsText = $"{latestConnectionError}{Environment.NewLine}{latestError}";
					else
						this.ConnectionErrorsText = latestConnectionError;
				}
				else if (!string.IsNullOrWhiteSpace(latestConnectionError) && string.IsNullOrWhiteSpace(latestError))
					this.ConnectionErrorsText = latestConnectionError;
				else if (string.IsNullOrWhiteSpace(latestConnectionError) && !string.IsNullOrWhiteSpace(latestError))
					this.ConnectionErrorsText = latestError;
				else
					this.ConnectionErrorsText = string.Empty;
				
				this.HasConnectionErrors = !string.IsNullOrWhiteSpace(this.ConnectionErrorsText);
				this.EvaluateCommands(this.ViewMyContactsCommand, this.ViewMyThingsCommand, this.ScanQrCodeCommand,
					this.ViewSignedContractsCommand, this.ViewWalletCommand);
			}
			catch (Exception ex)
			{
				this.logService?.LogException(ex);
			}
		}

		internal async Task SharePhoto()
		{
			try
			{
				if (this.ImageBin is null)
					return;

				IShareContent shareContent = DependencyService.Get<IShareContent>();
				string FileName = "Photo." + InternetContent.GetFileExtension(this.ImageContentType);

				shareContent.ShareImage(this.ImageBin, this.FullName, AppResources.Share, FileName);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		internal async Task ShareQR()
		{
			try
			{
				if (this.QrCodeBin is null)
					return;

				IShareContent shareContent = DependencyService.Get<IShareContent>();
				string FileName = "QR." + InternetContent.GetFileExtension(this.QrCodeContentType);

				shareContent.ShareImage(this.QrCodeBin, this.FullName, AppResources.Share, FileName);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}
	}
}