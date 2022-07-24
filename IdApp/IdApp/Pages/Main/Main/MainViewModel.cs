using IdApp.DeviceSpecific;
using IdApp.Extensions;
using IdApp.Pages.Contacts;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.MyContracts;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Resx;
using IdApp.Services.Data.Countries;
using IdApp.Services.UI.Photos;
using IdApp.Services.Xmpp;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Main
{
	/// <summary>
	/// The view model to bind to for the main page of the application. Holds basic user profile state
	/// as well as connection state.
	/// </summary>
	public class MainViewModel : QrXmppViewModel
	{
		private readonly PhotosLoader photosLoader;
		private readonly MainPage mainPage;

		/// <summary>
		/// Creates a new instance of the <see cref="MainViewModel"/> class.
		/// </summary>
		/// <param name="MainPage">Main Page</param>
		protected internal MainViewModel(MainPage MainPage)
			: base()
		{
			this.mainPage = MainPage;
			this.photosLoader = new PhotosLoader();

			this.ViewMyIdentityCommand = new Command(async () => await this.ViewMyIdentity(), () => this.IsConnected);
			this.ViewMyContactsCommand = new Command(async () => await this.ViewMyContacts(), () => this.IsConnected);
			this.ViewMyThingsCommand = new Command(async () => await this.ViewMyThings(), () => this.IsConnected);
			this.ScanQrCodeCommand = new Command(async () => await this.ScanQrCode());
			this.ViewSignedContractsCommand = new Command(async () => await this.ViewSignedContracts(), () => this.IsConnected);
			this.ViewWalletCommand = new Command(async () => await this.ViewWallet(), () => this.IsConnected);
			this.SharePhotoCommand = new Command(async () => await this.SharePhoto());
			this.ShareQRCommand = new Command(async () => await this.ShareQR());
		}

		/// <inheritdoc />
		protected override async Task DoBind()
		{
			await base.DoBind();

			this.AssignProperties();
			this.SetConnectionStateAndText(this.XmppService.State);
			this.XmppService.ConnectionStateChanged += this.Contracts_ConnectionStateChanged;
			this.NetworkService.ConnectivityChanged += this.NetworkService_ConnectivityChanged;
		}

		/// <inheritdoc />
		protected override Task DoUnbind()
		{
			this.photosLoader.CancelLoadPhotos();
			this.XmppService.ConnectionStateChanged -= this.Contracts_ConnectionStateChanged;
			this.NetworkService.ConnectivityChanged -= this.NetworkService_ConnectivityChanged;

			return base.DoUnbind();
		}

		private void AssignProperties()
		{
			if (this.TagProfile?.LegalIdentity is not null)
			{
				string FirstName = this.TagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
				string LastNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.LastName];

				if (!string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastNames))
					this.FullName = FirstName + " " + LastNames;
				else if (!string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastNames))
					this.FullName = FirstName;
				else if (string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastNames))
					this.FullName = LastNames;
				else
					this.FullName = string.Empty;

				this.City = this.TagProfile.LegalIdentity[Constants.XmppProperties.City];
				string CountryCode = this.TagProfile.LegalIdentity[Constants.XmppProperties.Country];

				if (ISO_3166_1.TryGetCountry(CountryCode, out string Country))
					this.Country = Country;
				else
					this.Country = string.Empty;

				this.GenerateQrCode(Constants.UriSchemes.CreateIdUri(this.TagProfile.LegalIdentity.Id));

				Attachment FirstAttachment = this.TagProfile.LegalIdentity.Attachments?.GetFirstImageAttachment();
				if (FirstAttachment is not null)
				{
					_ = Task.Run(async () =>
					{
						try
						{
							await this.LoadProfilePhoto(FirstAttachment);
						}
						catch (Exception ex)
						{
							this.LogService.LogException(ex);
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

				this.RemoveQrCode();
			}
		}

		private async Task LoadProfilePhoto(Attachment FirstAttachment)
		{
			try
			{
				bool Connected = await this.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
				if (!Connected)
					return;

				(byte[] Bin, string ContentType, int Rotation) = await this.photosLoader.LoadOnePhoto(FirstAttachment, SignWith.LatestApprovedIdOrCurrentKeys);

				this.ImageBin = Bin;
				this.ImageContentType = ContentType;

				if (Bin is not null)
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
				this.LogService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}
		}

		#region Properties

		/// <summary>
		/// See <see cref="ViewMyIdentityCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewMyIdentityCommandProperty =
			BindableProperty.Create(nameof(ViewMyIdentityCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ViewMyIdentityCommand
		{
			get => (ICommand)this.GetValue(ViewMyIdentityCommandProperty);
			set => this.SetValue(ViewMyIdentityCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="ViewMyContactsCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewMyContactsCommandProperty =
			BindableProperty.Create(nameof(ViewMyContactsCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ViewMyContactsCommand
		{
			get => (ICommand)this.GetValue(ViewMyContactsCommandProperty);
			set => this.SetValue(ViewMyContactsCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="ViewMyThingsCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewMyThingsCommandProperty =
			BindableProperty.Create(nameof(ViewMyThingsCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ViewMyThingsCommand
		{
			get => (ICommand)this.GetValue(ViewMyThingsCommandProperty);
			set => this.SetValue(ViewMyThingsCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="ScanQrCodeCommand"/>
		/// </summary>
		public static readonly BindableProperty ScanQrCodeCommandProperty =
			BindableProperty.Create(nameof(ScanQrCodeCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for scanning a QR code.
		/// </summary>
		public ICommand ScanQrCodeCommand
		{
			get => (ICommand)this.GetValue(ScanQrCodeCommandProperty);
			set => this.SetValue(ScanQrCodeCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="ViewSignedContractsCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewSignedContractsCommandProperty =
			BindableProperty.Create(nameof(ViewSignedContractsCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing a user's wallet.
		/// </summary>
		public ICommand ViewSignedContractsCommand
		{
			get => (ICommand)this.GetValue(ViewSignedContractsCommandProperty);
			set => this.SetValue(ViewSignedContractsCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="ViewWalletCommand"/>
		/// </summary>
		public static readonly BindableProperty ViewWalletCommandProperty =
			BindableProperty.Create(nameof(ViewWalletCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing a user's wallet.
		/// </summary>
		public ICommand ViewWalletCommand
		{
			get => (ICommand)this.GetValue(ViewWalletCommandProperty);
			set => this.SetValue(ViewWalletCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="SharePhotoCommand"/>
		/// </summary>
		public static readonly BindableProperty SharePhotoCommandProperty =
			BindableProperty.Create(nameof(SharePhotoCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand SharePhotoCommand
		{
			get => (ICommand)this.GetValue(SharePhotoCommandProperty);
			set => this.SetValue(SharePhotoCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="ShareQRCommand"/>
		/// </summary>
		public static readonly BindableProperty ShareQRCommandProperty =
			BindableProperty.Create(nameof(ShareQRCommand), typeof(ICommand), typeof(MainViewModel), default(ICommand));

		/// <summary>
		/// The command to bind to for viewing the user's own contracts.
		/// </summary>
		public ICommand ShareQRCommand
		{
			get => (ICommand)this.GetValue(ShareQRCommandProperty);
			set => this.SetValue(ShareQRCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty HasPhotoProperty =
			BindableProperty.Create(nameof(HasPhoto), typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public bool HasPhoto
		{
			get => (bool)this.GetValue(HasPhotoProperty);
			set => this.SetValue(HasPhotoProperty, value);
		}

		/// <summary>
		/// See <see cref="Image"/>
		/// </summary>
		public static readonly BindableProperty ImageProperty =
			BindableProperty.Create(nameof(Image), typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
				viewModel.HasPhoto = (newValue is not null);
			});

		/// <summary>
		/// Gets or sets the current user's photo.
		/// </summary>
		public ImageSource Image
		{
			get => (ImageSource)this.GetValue(ImageProperty);
			set => this.SetValue(ImageProperty, value);
		}

		/// <summary>
		/// See <see cref="ImageRotation"/>
		/// </summary>
		public static readonly BindableProperty ImageRotationProperty =
			BindableProperty.Create(nameof(ImageRotation), typeof(int), typeof(MainViewModel), default(int));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public int ImageRotation
		{
			get => (int)this.GetValue(ImageRotationProperty);
			set => this.SetValue(ImageRotationProperty, value);
		}

		/// <summary>
		/// See <see cref="ImageBin"/>
		/// </summary>
		public static readonly BindableProperty ImageBinProperty =
			BindableProperty.Create(nameof(ImageBin), typeof(byte[]), typeof(MainViewModel), default(byte[]));

		/// <summary>
		/// Binary encoding of photo
		/// </summary>
		public byte[] ImageBin
		{
			get { return (byte[])this.GetValue(ImageBinProperty); }
			set => this.SetValue(ImageBinProperty, value);
		}

		/// <summary>
		/// See <see cref="ImageContentType"/>
		/// </summary>
		public static readonly BindableProperty ImageContentTypeProperty =
			BindableProperty.Create(nameof(ImageContentType), typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Content-Type of photo
		/// </summary>
		public string ImageContentType
		{
			get => (string)this.GetValue(ImageContentTypeProperty);
			set => this.SetValue(ImageContentTypeProperty, value);
		}

		/// <summary>
		/// See <see cref="FullName"/>
		/// </summary>
		public static readonly BindableProperty FullNameProperty =
			BindableProperty.Create(nameof(FullName), typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the current user's full name.
		/// </summary>
		public string FullName
		{
			get => (string)this.GetValue(FullNameProperty);
			set => this.SetValue(FullNameProperty, value);
		}

		/// <summary>
		/// See <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create(nameof(City), typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
				viewModel.SetLocation();
			});

		/// <summary>
		/// Gets or sets the current user's city.
		/// </summary>
		public string City
		{
			get => (string)this.GetValue(CityProperty);
			set => this.SetValue(CityProperty, value);
		}

		/// <summary>
		/// See <see cref="Country"/>
		/// </summary>
		public static readonly BindableProperty CountryProperty =
			BindableProperty.Create(nameof(Country), typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				MainViewModel viewModel = (MainViewModel)b;
				viewModel.SetLocation();
			});

		/// <summary>
		/// Gets or sets the current user's country.
		/// </summary>
		public string Country
		{
			get => (string)this.GetValue(CountryProperty);
			set => this.SetValue(CountryProperty, value);
		}

		/// <summary>
		/// See <see cref="Location"/>
		/// </summary>
		public static readonly BindableProperty LocationProperty =
			BindableProperty.Create(nameof(Location), typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the current user's location.
		/// </summary>
		public string Location
		{
			get => (string)this.GetValue(LocationProperty);
			set => this.SetValue(LocationProperty, value);
		}

		/// <summary>
		/// See <see cref="IsOnline"/>
		/// </summary>
		public static readonly BindableProperty IsOnlineProperty =
			BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the app is currently online, i.e. has network access.
		/// </summary>
		public bool IsOnline
		{
			get => (bool)this.GetValue(IsOnlineProperty);
			set => this.SetValue(IsOnlineProperty, value);
		}

		/// <summary>
		/// See <see cref="NetworkStateText"/>
		/// </summary>
		public static readonly BindableProperty NetworkStateTextProperty =
			BindableProperty.Create(nameof(NetworkStateText), typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the user friendly network state text for display.
		/// </summary>
		public string NetworkStateText
		{
			get => (string)this.GetValue(NetworkStateTextProperty);
			set => this.SetValue(NetworkStateTextProperty, value);
		}

		/// <summary>
		/// See <see cref="IdentityStateText"/>
		/// </summary>
		public static readonly BindableProperty IdentityStateTextProperty =
			BindableProperty.Create(nameof(IdentityStateText), typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the user friendly network state text for display.
		/// </summary>
		public string IdentityStateText
		{
			get => (string)this.GetValue(IdentityStateTextProperty);
			set => this.SetValue(IdentityStateTextProperty, value);
		}

		/// <summary>
		/// See <see cref="HasConnectionErrors"/>
		/// </summary>
		public static readonly BindableProperty HasConnectionErrorsProperty =
			BindableProperty.Create(nameof(HasConnectionErrors), typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether there are any connection errors at all for the app.
		/// </summary>
		public bool HasConnectionErrors
		{
			get => (bool)this.GetValue(HasConnectionErrorsProperty);
			set => this.SetValue(HasConnectionErrorsProperty, value);
		}

		/// <summary>
		/// See <see cref="ConnectionErrorsText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionErrorsTextProperty =
			BindableProperty.Create(nameof(ConnectionErrorsText), typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets the user friendly connection errors text for display. Can be null.
		/// </summary>
		public string ConnectionErrorsText
		{
			get => (string)this.GetValue(ConnectionErrorsTextProperty);
			set => this.SetValue(ConnectionErrorsTextProperty, value);
		}

		#endregion

		private async Task ViewMyIdentity()
		{
			if (!await App.VerifyPin())
				return;

			await this.NavigationService.GoToAsync(nameof(ViewIdentityPage));
		}

		private async Task ViewMyContacts()
		{
			await this.NavigationService.GoToAsync(nameof(MyContactsPage),
				new ContactListNavigationArgs(AppResources.ContactsDescription, SelectContactAction.ViewIdentity));
		}

		private async Task ViewMyThings()
		{
			await this.NavigationService.GoToAsync(nameof(Things.MyThings.MyThingsPage));
		}

		private async Task ViewSignedContracts()
		{
			await this.NavigationService.GoToAsync(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.SignedContracts));
		}

		private async Task ViewWallet()
		{
			await this.NeuroWalletOrchestratorService.OpenWallet();
		}

		private async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult();
		}

		private void SetLocation()
		{
			if (!string.IsNullOrWhiteSpace(this.City) && !string.IsNullOrWhiteSpace(this.Country))
				this.Location = this.City + ", " + this.Country;
			else if (!string.IsNullOrWhiteSpace(this.City) && string.IsNullOrWhiteSpace(this.Country))
				this.Location = this.City;
			else if (string.IsNullOrWhiteSpace(this.City) && !string.IsNullOrWhiteSpace(this.Country))
				this.Location = this.Country;
		}

		/// <inheritdoc />
		protected override void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
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
				this.SetConnectionStateAndText(this.XmppService.State);
			});
		}

		private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() => this.SetConnectionStateAndText(this.XmppService.State));
		}

		/// <inheritdoc/>
		protected override void SetConnectionStateAndText(XmppState state)
		{
			try
			{
				// Network
				this.IsOnline = this.NetworkService?.IsOnline ?? false;
				this.NetworkStateText = this.IsOnline ? AppResources.Online : AppResources.Offline;
				this.IdentityStateText = this.TagProfile?.LegalIdentity?.State.ToDisplayText() ?? string.Empty;

				// XMPP server
				this.IsConnected = state == XmppState.Connected;
				this.ConnectionStateText = state.ToDisplayText();
				this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
				this.StateSummaryText = (this.TagProfile?.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;

				// Any connection errors or general errors that should be displayed?
				string LatestError = this.XmppService?.LatestError ?? string.Empty;
				string LatestConnectionError = this.XmppService?.LatestConnectionError ?? string.Empty;

				if (!string.IsNullOrWhiteSpace(LatestError) && !string.IsNullOrWhiteSpace(LatestConnectionError))
				{
					if (LatestConnectionError != LatestError)
						this.ConnectionErrorsText = LatestConnectionError + Environment.NewLine + LatestError;
					else
						this.ConnectionErrorsText = LatestConnectionError;
				}
				else if (!string.IsNullOrWhiteSpace(LatestConnectionError) && string.IsNullOrWhiteSpace(LatestError))
					this.ConnectionErrorsText = LatestConnectionError;
				else if (string.IsNullOrWhiteSpace(LatestConnectionError) && !string.IsNullOrWhiteSpace(LatestError))
					this.ConnectionErrorsText = LatestError;
				else
					this.ConnectionErrorsText = string.Empty;

				this.HasConnectionErrors = !string.IsNullOrWhiteSpace(this.ConnectionErrorsText);
				this.EvaluateCommands(this.ViewMyIdentityCommand, this.ViewMyContactsCommand, this.ViewMyThingsCommand,
					this.ScanQrCodeCommand, this.ViewSignedContractsCommand, this.ViewWalletCommand);
			}
			catch (Exception ex)
			{
				this.LogService?.LogException(ex);
			}
		}

		internal async Task SharePhoto()
		{
			try
			{
				if (this.ImageBin is null)
					return;

				IShareContent ShareContent = DependencyService.Get<IShareContent>();
				string FileName = "Photo." + InternetContent.GetFileExtension(this.ImageContentType);

				ShareContent.ShareImage(this.ImageBin, this.FullName, AppResources.Share, FileName);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		internal async Task ShareQR()
		{
			try
			{
				if (this.QrCodeBin is null)
					return;

				IShareContent ShareContent = DependencyService.Get<IShareContent>();
				string FileName = "QR." + InternetContent.GetFileExtension(this.QrCodeContentType);

				ShareContent.ShareImage(this.QrCodeBin, this.FullName, AppResources.Share, FileName);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		internal async Task CheckOtpTimestamp()
		{
			if (this.TagProfile.TestOtpTimestamp is not null)
			{
				TimeSpan TimeSpan = DateTime.Now - this.TagProfile.TestOtpTimestamp.Value;

				if (TimeSpan.TotalDays >= 7)
				{
					try
					{
						(bool Succeeded, LegalIdentity RevokedIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.ObsoleteLegalIdentity(this.TagProfile.LegalIdentity.Id));
						if (Succeeded)
						{
							this.TagProfile.RevokeLegalIdentity(RevokedIdentity);
						}
					}
					catch (Exception ex)
					{
						this.LogService.LogException(ex);
						await this.UiSerializer.DisplayAlert(ex);
					}
				}
			}
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(this.FullName);

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public override byte[] Media
		{
			get
			{
				if (this.mainPage.IsFrontViewShowing && this.HasPhoto)
					return this.ImageBin;
				else
					return base.Media;
			}
		}

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public override string MediaContentType
		{
			get
			{
				if (this.mainPage.IsFrontViewShowing && this.HasPhoto)
					return this.ImageContentType;
				else
					return base.MediaContentType;
			}
		}

		#endregion

	}
}
