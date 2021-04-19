﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Services;
using IdApp.Views.Contacts;
using IdApp.Views.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Images.Exif;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
	/// <summary>
	/// The view model to bind to for the main page of the application. Holds basic user profile state
	/// as well as connection state.
	/// </summary>
	public class MainViewModel : NeuronViewModel
	{
		private readonly ITagProfile tagProfile;
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
			IUiDispatcher uiDispatcher,
			ITagProfile tagProfile,
			INavigationService navigationService,
			INetworkService networkService,
			IAttachmentCacheService attachmentCacheService,
			IContractOrchestratorService contractOrchestratorService,
			IThingRegistryOrchestratorService thingThingRegistryOrchestratorService,
			IEDalerOrchestratorService eDalerOrchestratorService)
			: base(neuronService ?? Types.Instantiate<INeuronService>(false),
				  uiDispatcher ?? Types.Instantiate<IUiDispatcher>(false))
		{
			this.logService = logService ?? Types.Instantiate<ILogService>(false);
			this.tagProfile = tagProfile ?? Types.Instantiate<ITagProfile>(false);
			this.navigationService = navigationService ?? Types.Instantiate<INavigationService>(false);
			this.networkService = networkService ?? Types.Instantiate<INetworkService>(false);
			this.contractOrchestratorService = contractOrchestratorService ?? Types.Instantiate<IContractOrchestratorService>(false);
			this.thingRegistryOrchestratorService = thingThingRegistryOrchestratorService ?? Types.Instantiate<IThingRegistryOrchestratorService>(false);
			this.eDalerOrchestratorService = eDalerOrchestratorService ?? Types.Instantiate<IEDalerOrchestratorService>(false);

			this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.NeuronService, this.UiDispatcher,
				attachmentCacheService ?? Types.Instantiate<IAttachmentCacheService>(false));
			this.UpdateLoggedOutText(true);

			this.ViewMyContactsCommand = new Command(async () => await ViewMyContacts(), () => this.IsConnected);
			this.ViewMyThingsCommand = new Command(async () => await ViewMyThings(), () => this.IsConnected);
			this.ScanQrCodeCommand = new Command(async () => await ScanQrCode());
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
			if (!(this.tagProfile?.LegalIdentity is null))
			{
				string firstName = this.tagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
				string lastNames = this.tagProfile.LegalIdentity[Constants.XmppProperties.LastName];

				if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
					this.FullName = $"{firstName} {lastNames}";
				else if (!string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastNames))
					this.FullName = firstName;
				else if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
					this.FullName = lastNames;
				else
					this.FullName = string.Empty;

				this.City = this.tagProfile.LegalIdentity[Constants.XmppProperties.City];
				string countryCode = this.tagProfile.LegalIdentity[Constants.XmppProperties.Country];

				if (ISO_3166_1.TryGetCountry(countryCode, out string country))
					this.Country = country;
				else
					this.Country = string.Empty;
			}
			else
			{
				this.FullName = string.Empty;
				this.City = string.Empty;
				this.Country = string.Empty;
			}

			// QR
			if (!(this.tagProfile?.LegalIdentity is null))
			{
				_ = Task.Run(() =>
				{
					this.QrCodeBin = QrCodeImageGenerator.GeneratePng(Constants.UriSchemes.CreateIdUri(this.tagProfile.LegalIdentity.Id), this.QrCodeWidth, this.QrCodeHeight);
					this.QrCodeContentType = "image/png";

					if (this.IsBound)
					{
						this.UiDispatcher.BeginInvokeOnMainThread(() => this.QrCode = ImageSource.FromStream(() => new MemoryStream(this.QrCodeBin)));
					}
				});
			}
			else
			{
				this.QrCode = null;
				this.QrCodeBin = null;
				this.QrCodeContentType = string.Empty;
			}

			Attachment firstAttachment = this.tagProfile?.LegalIdentity?.Attachments?.GetFirstImageAttachment();
			if (!(firstAttachment is null))
			{
				// Don't await this one, just let it run asynchronously.
				_ = this.LoadProfilePhoto(firstAttachment);
			}
		}

		private async Task LoadProfilePhoto(Attachment firstAttachment)
		{
			try
			{
				bool connected = await this.NeuronService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
				if (!connected)
					return;

				(byte[] Bin, string ContentType) = await this.photosLoader.LoadOnePhoto(firstAttachment, SignWith.LatestApprovedIdOrCurrentKeys);

				this.ImageBin = Bin;
				this.ImageContentType = ContentType;

				if (Bin != null)
				{
					this.UiDispatcher.BeginInvokeOnMainThread(() =>
					{
						if (this.IsBound)
						{
							this.ImageRotation = GetImageRotation(Bin);
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

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="JpegImage">Binary representation of JPEG image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(byte[] JpegImage)
		{
			if (JpegImage is null)
				return 0;

			if (!EXIF.TryExtractFromJPeg(JpegImage, out ExifTag[] Tags))
				return 0;

			foreach (ExifTag Tag in Tags)
			{
				if (Tag.Name == ExifTagName.Orientation)
				{
					if (Tag.Value is ushort Orientation)
					{
						switch (Orientation)
						{
							case 1: return 0;		// Top left. Default orientation.
							case 2: return 0;		// Top right. Horizontally reversed.
							case 3: return 180;		// Bottom right. Rotated by 180 degrees.
							case 4: return 180;		// Bottom left. Rotated by 180 degrees and then horizontally reversed.
							case 5: return -90;		// Left top. Rotated by 90 degrees counterclockwise and then horizontally reversed.
							case 6: return 90;		// Right top. Rotated by 90 degrees clockwise.
							case 7: return 90;		// Right bottom. Rotated by 90 degrees clockwise and then horizontally reversed.
							case 8: return -90;		// Left bottom. Rotated by 90 degrees counterclockwise.
							default: return 0;
						}
					}
				}
			}

			return 0;
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

		/// <summary>
		/// See <see cref="YouAreNowLoggedOutText"/>
		/// </summary>
		public static readonly BindableProperty YouAreNowLoggedOutTextProperty =
			BindableProperty.Create("YouAreNowLoggedOutText", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// The text to display in the UI, on the logout panel, depending on whether the user has manually logged out or in.
		/// </summary>
		public string YouAreNowLoggedOutText
		{
			get { return (string)GetValue(YouAreNowLoggedOutTextProperty); }
			set { SetValue(YouAreNowLoggedOutTextProperty, value); }
		}

		#endregion

		private async Task ViewMyContacts()
		{
			await this.navigationService.GoToAsync(nameof(MyContactsPage));
		}

		private async Task ViewMyThings()
		{
			await this.navigationService.GoToAsync(nameof(MyThingsPage));
		}

		private async Task ViewWallet()
		{
			await this.eDalerOrchestratorService.OpenWallet();
		}

		private async Task ScanQrCode()
		{
			await IdApp.QrCode.ScanQrCodeAndHandleResult(this.logService, this.NeuronService, this.navigationService,
				this.UiDispatcher, this.contractOrchestratorService, this.thingRegistryOrchestratorService,
				this.eDalerOrchestratorService);
		}

		private void UpdateLoggedOutText(bool isLoggedOut)
		{
			this.YouAreNowLoggedOutText = isLoggedOut ? AppResources.YouHaveNowBeenSignedOut : AppResources.SigningIn;
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
			this.UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				if (e.IsUserInitiated)
				{
					this.UpdateLoggedOutText(e.State == XmppState.Offline);
				}
			});
		}

		private void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(this.NeuronService.State);
			});
		}

		private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(() => SetConnectionStateAndText(this.NeuronService.State));
		}

		/// <inheritdoc/>
		protected override void SetConnectionStateAndText(XmppState state)
		{
			// Network
			this.IsOnline = this.networkService.IsOnline;
			this.NetworkStateText = this.IsOnline ? AppResources.Online : AppResources.Offline;

			// Neuron server
			this.IsConnected = state == XmppState.Connected;
			this.ConnectionStateText = state.ToDisplayText(this.tagProfile);

			// Any connection errors or general errors that should be displayed?
			string latestError = this.NeuronService.LatestError;
			string latestConnectionError = this.NeuronService.LatestConnectionError;
			if (!string.IsNullOrWhiteSpace(latestError) && !string.IsNullOrWhiteSpace(latestConnectionError))
			{
				if (latestConnectionError != latestError)
					this.ConnectionErrorsText = $"{latestConnectionError}{Environment.NewLine}{latestError}";
				else
					this.ConnectionErrorsText = latestConnectionError;
			}
			else if (!string.IsNullOrWhiteSpace(latestConnectionError) && string.IsNullOrWhiteSpace(latestError))
			{
				this.ConnectionErrorsText = latestConnectionError;
			}
			else if (string.IsNullOrWhiteSpace(latestConnectionError) && !string.IsNullOrWhiteSpace(latestError))
			{
				this.ConnectionErrorsText = latestError;
			}
			else
			{
				this.ConnectionErrorsText = string.Empty;
			}
			this.HasConnectionErrors = !string.IsNullOrWhiteSpace(this.ConnectionErrorsText);
			this.EvaluateCommands(this.ViewMyContactsCommand, this.ViewMyThingsCommand, this.ScanQrCodeCommand, this.ViewWalletCommand);
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
				await this.UiDispatcher.DisplayAlert(ex);
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
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}
	}
}