using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Linear;
using IdApp.Cv.Utilities;
using IdApp.DeviceSpecific;
using IdApp.Extensions;
using IdApp.Services.Data.PersonalNumbers;
using IdApp.Services.Tag;
using IdApp.Services.UI.Photos;
using IdApp.Services.Data.Countries;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Images.Exif;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;
using IdApp.Services.Ocr;
using IdApp.Nfc.Extensions;
using IdApp.Cv.Arithmetics;
using SkiaSharp;
using IdApp.Services;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Registration.RegisterIdentity
{
	/// <summary>
	/// The view model to bind to when showing Step 3 of the registration flow: registering an identity.
	/// </summary>
	public class RegisterIdentityViewModel : RegistrationStepViewModel
	{
		private const string profilePhotoFileName = "ProfilePhoto.jpg";
		private readonly string localPhotoFileName;
		private LegalIdentityAttachment photo;
		private readonly PhotosLoader photosLoader;

		/// <summary>
		/// Creates a new instance of the <see cref="RegisterIdentityModel"/> class.
		/// </summary>
		public RegisterIdentityViewModel()
		 : base(RegistrationStep.RegisterIdentity)
		{
			IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();
			this.DeviceId = deviceInfo?.GetDeviceId();

			this.Countries = new ObservableCollection<string>();
			foreach (string country in ISO_3166_1.Countries)
				this.Countries.Add(country);

			this.SelectedCountry = null;
			this.RegisterCommand = new Command(async _ => await this.Register(), _ => this.CanRegister());
			this.TakePhotoCommand = new Command(async _ => await this.TakePhoto(), _ => !this.IsBusy);
			this.PickPhotoCommand = new Command(async _ => await this.PickPhoto(), _ => !this.IsBusy);
			this.EPassportCommand = new Command(async _ => await this.ScanPassport(), _ => !this.IsBusy);
			this.RemovePhotoCommand = new Command(_ => this.RemovePhoto(true));

			this.Title = LocalizationResourceManager.Current["PersonalLegalInformation"];
			this.PersonalNumberPlaceholder = LocalizationResourceManager.Current["PersonalNumber"];
			this.ShowOrganization = this.TagProfile.Purpose == PurposeUse.Work;

			this.localPhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), profilePhotoFileName);
			this.photosLoader = new PhotosLoader();
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.RegisterCommand.ChangeCanExecute();
			this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.photosLoader.CancelLoadPhotos();
			this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// True if the user choose the educational or experimental purpose.
		/// </summary>
		public bool IsTest => this.TagProfile.IsTest;

		/// <summary>
		/// Purpose for using the app.
		/// </summary>
		public PurposeUse Purpose => this.TagProfile.Purpose;

		/// <summary>
		/// The command to bind to for performing the 'register' action.
		/// </summary>
		public ICommand RegisterCommand { get; }

		/// <summary>
		/// The command to bind to for taking a photo with the camera.
		/// </summary>
		public ICommand TakePhotoCommand { get; }

		/// <summary>
		/// The command to bind to for selecting a photo from the camera roll.
		/// </summary>
		public ICommand PickPhotoCommand { get; }

		/// <summary>
		/// The command to bind to for scanning an ePassport or eID.
		/// </summary>
		public ICommand EPassportCommand { get; }

		/// <summary>
		/// The command to bind to for removing the currently selected photo.
		/// </summary>
		public ICommand RemovePhotoCommand { get; }

		/// <summary>
		/// The <see cref="ShowOrganization"/>
		/// </summary>
		public static readonly BindableProperty ShowOrganizationProperty =
			BindableProperty.Create(nameof(ShowOrganization), typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the user has selected a photo for their account or not.
		/// </summary>
		public bool ShowOrganization
		{
			get => (bool)this.GetValue(ShowOrganizationProperty);
			set => this.SetValue(ShowOrganizationProperty, value);
		}

		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty HasPhotoProperty =
			BindableProperty.Create(nameof(HasPhoto), typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the user has selected a photo for their account or not.
		/// </summary>
		public bool HasPhoto
		{
			get => (bool)this.GetValue(HasPhotoProperty);
			set => this.SetValue(HasPhotoProperty, value);
		}

		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty ImageProperty =
			BindableProperty.Create(nameof(Image), typeof(ImageSource), typeof(RegisterIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
				viewModel.HasPhoto = (newValue is not null);
			});

		/// <summary>
		/// The image source, i.e. the file representing the selected photo.
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
			BindableProperty.Create(nameof(ImageRotation), typeof(int), typeof(Main.Main.MainViewModel), default(int));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public int ImageRotation
		{
			get => (int)this.GetValue(ImageRotationProperty);
			set => this.SetValue(ImageRotationProperty, value);
		}

		/// <summary>
		/// The list of all available countries a user can select from.
		/// </summary>
		public ObservableCollection<string> Countries { get; }

		/// <summary>
		/// The <see cref="SelectedCountry"/>
		/// </summary>
		public static readonly BindableProperty SelectedCountryProperty =
			BindableProperty.Create(nameof(SelectedCountry), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				RegisterIdentityViewModel ViewModel = (RegisterIdentityViewModel)b;
				ViewModel.RegisterCommand.ChangeCanExecute();

				if (!string.IsNullOrWhiteSpace(ViewModel.SelectedCountry) &&
					ISO_3166_1.TryGetCode(ViewModel.SelectedCountry, out string CountryCode))
				{
					string format = PersonalNumberSchemes.DisplayStringForCountry(CountryCode);
					if (!string.IsNullOrWhiteSpace(format))
						ViewModel.PersonalNumberPlaceholder = string.Format(LocalizationResourceManager.Current["PersonalNumberPlaceholder"], format);
					else
						ViewModel.PersonalNumberPlaceholder = LocalizationResourceManager.Current["PersonalNumber"];
				}
				else
					ViewModel.PersonalNumberPlaceholder = LocalizationResourceManager.Current["PersonalNumber"];
			});

		/// <summary>
		/// The user selected country from the list of <see cref="Countries"/>.
		/// </summary>
		public string SelectedCountry
		{
			get => (string)this.GetValue(SelectedCountryProperty);
			set
			{
				string Prev = this.SelectedCountry;
				if (Prev != value)
				{
					this.SetValue(SelectedCountryProperty, value);
					if (Prev == this.SelectedOrgCountry)
						this.SelectedOrgCountry = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="SelectedOrgCountry"/>
		/// </summary>
		public static readonly BindableProperty SelectedOrgCountryProperty =
			BindableProperty.Create(nameof(SelectedOrgCountry), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user selected organization country from the list of <see cref="Countries"/>.
		/// </summary>
		public string SelectedOrgCountry
		{
			get => (string)this.GetValue(SelectedOrgCountryProperty);
			set => this.SetValue(SelectedOrgCountryProperty, value);
		}

		/// <summary>
		/// The <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create(nameof(FirstName), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's first name
		/// </summary>
		public string FirstName
		{
			get => (string)this.GetValue(FirstNameProperty);
			set => this.SetValue(FirstNameProperty, value);
		}

		/// <summary>
		/// The <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's middle name(s)
		/// </summary>
		public string MiddleNames
		{
			get => (string)this.GetValue(MiddleNamesProperty);
			set => this.SetValue(MiddleNamesProperty, value);
		}

		/// <summary>
		/// The <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create(nameof(LastNames), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's last name(s)
		/// </summary>
		public string LastNames
		{
			get => (string)this.GetValue(LastNamesProperty);
			set => this.SetValue(LastNamesProperty, value);
		}

		/// <summary>
		/// The <see cref="OrgName"/>
		/// </summary>
		public static readonly BindableProperty OrgNameProperty =
			BindableProperty.Create(nameof(OrgName), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The organization name
		/// </summary>
		public string OrgName
		{
			get => (string)this.GetValue(OrgNameProperty);
			set => this.SetValue(OrgNameProperty, value);
		}

		/// <summary>
		/// The <see cref="PersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberProperty =
			BindableProperty.Create(nameof(PersonalNumber), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's personal number
		/// </summary>
		public string PersonalNumber
		{
			get => (string)this.GetValue(PersonalNumberProperty);
			set => this.SetValue(PersonalNumberProperty, value);
		}

		/// <summary>
		/// The <see cref="PersonalNumberPlaceholder"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberPlaceholderProperty =
			BindableProperty.Create(nameof(PersonalNumberPlaceholder), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The personal number placeholder, used as a guide to the user to enter the correct format, which depends on the <see cref="SelectedCountry"/>.
		/// </summary>
		public string PersonalNumberPlaceholder
		{
			get => (string)this.GetValue(PersonalNumberPlaceholderProperty);
			set => this.SetValue(PersonalNumberPlaceholderProperty, value);
		}

		/// <summary>
		/// The <see cref="OrgNumber"/>
		/// </summary>
		public static readonly BindableProperty OrgNumberProperty =
			BindableProperty.Create(nameof(OrgNumber), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The organization number
		/// </summary>
		public string OrgNumber
		{
			get => (string)this.GetValue(OrgNumberProperty);
			set => this.SetValue(OrgNumberProperty, value);
		}

		/// <summary>
		/// The <see cref="Address"/>
		/// </summary>
		public static readonly BindableProperty AddressProperty =
			BindableProperty.Create(nameof(Address), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's address, line 1.
		/// </summary>
		public string Address
		{
			get => (string)this.GetValue(AddressProperty);
			set
			{
				string Prev = this.Address;
				if (Prev != value)
				{
					this.SetValue(AddressProperty, value);
					if (Prev == this.OrgAddress)
						this.OrgAddress = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="Address2"/>
		/// </summary>
		public static readonly BindableProperty Address2Property =
			BindableProperty.Create(nameof(Address2), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's address, line 2.
		/// </summary>
		public string Address2
		{
			get => (string)this.GetValue(Address2Property);
			set
			{
				string Prev = this.Address2;
				if (Prev != value)
				{
					this.SetValue(Address2Property, value);
					if (Prev == this.OrgAddress2)
						this.OrgAddress2 = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="OrgAddress"/>
		/// </summary>
		public static readonly BindableProperty OrgAddressProperty =
			BindableProperty.Create(nameof(OrgAddress), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The organization address, line 1.
		/// </summary>
		public string OrgAddress
		{
			get => (string)this.GetValue(OrgAddressProperty);
			set => this.SetValue(OrgAddressProperty, value);
		}

		/// <summary>
		/// The <see cref="OrgAddress2"/>
		/// </summary>
		public static readonly BindableProperty OrgAddress2Property =
			BindableProperty.Create(nameof(OrgAddress2), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The organization address, line 2.
		/// </summary>
		public string OrgAddress2
		{
			get => (string)this.GetValue(OrgAddress2Property);
			set => this.SetValue(OrgAddress2Property, value);
		}

		/// <summary>
		/// The <see cref="ZipCode"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeProperty =
			BindableProperty.Create(nameof(ZipCode), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's zip code
		/// </summary>
		public string ZipCode
		{
			get => (string)this.GetValue(ZipCodeProperty);
			set
			{
				string Prev = this.ZipCode;
				if (Prev != value)
				{
					this.SetValue(ZipCodeProperty, value);
					if (Prev == this.OrgZipCode)
						this.OrgZipCode = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="OrgZipCode"/>
		/// </summary>
		public static readonly BindableProperty OrgZipCodeProperty =
			BindableProperty.Create(nameof(OrgZipCode), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The organization zip code
		/// </summary>
		public string OrgZipCode
		{
			get => (string)this.GetValue(OrgZipCodeProperty);
			set => this.SetValue(OrgZipCodeProperty, value);
		}

		/// <summary>
		/// The <see cref="Area"/>
		/// </summary>
		public static readonly BindableProperty AreaProperty =
			BindableProperty.Create(nameof(Area), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's area
		/// </summary>
		public string Area
		{
			get => (string)this.GetValue(AreaProperty);
			set
			{
				string Prev = this.Area;
				if (Prev != value)
				{
					this.SetValue(AreaProperty, value);
					if (Prev == this.OrgArea)
						this.OrgArea = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="OrgArea"/>
		/// </summary>
		public static readonly BindableProperty OrgAreaProperty =
			BindableProperty.Create(nameof(OrgArea), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The organization area
		/// </summary>
		public string OrgArea
		{
			get => (string)this.GetValue(OrgAreaProperty);
			set => this.SetValue(OrgAreaProperty, value);
		}

		/// <summary>
		/// The <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create(nameof(City), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's city
		/// </summary>
		public string City
		{
			get => (string)this.GetValue(CityProperty);
			set
			{
				string Prev = this.City;
				if (Prev != value)
				{
					this.SetValue(CityProperty, value);
					if (Prev == this.OrgCity)
						this.OrgCity = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="OrgCity"/>
		/// </summary>
		public static readonly BindableProperty OrgCityProperty =
			BindableProperty.Create(nameof(OrgCity), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The organization city
		/// </summary>
		public string OrgCity
		{
			get => (string)this.GetValue(OrgCityProperty);
			set => this.SetValue(OrgCityProperty, value);
		}

		/// <summary>
		/// The <see cref="Region"/>
		/// </summary>
		public static readonly BindableProperty RegionProperty =
			BindableProperty.Create(nameof(Region), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's region
		/// </summary>
		public string Region
		{
			get => (string)this.GetValue(RegionProperty);
			set
			{
				string Prev = this.Region;
				if (Prev != value)
				{
					this.SetValue(RegionProperty, value);
					if (Prev == this.OrgRegion)
						this.OrgRegion = value;
				}
			}
		}

		/// <summary>
		/// The <see cref="OrgRegion"/>
		/// </summary>
		public static readonly BindableProperty OrgRegionProperty =
			BindableProperty.Create(nameof(OrgRegion), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The organization region
		/// </summary>
		public string OrgRegion
		{
			get => (string)this.GetValue(OrgRegionProperty);
			set => this.SetValue(OrgRegionProperty, value);
		}

		/// <summary>
		/// The <see cref="OrgDepartment"/>
		/// </summary>
		public static readonly BindableProperty OrgDepartmentProperty =
			BindableProperty.Create(nameof(OrgDepartment), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The organization department
		/// </summary>
		public string OrgDepartment
		{
			get => (string)this.GetValue(OrgDepartmentProperty);
			set => this.SetValue(OrgDepartmentProperty, value);
		}

		/// <summary>
		/// The <see cref="OrgRole"/>
		/// </summary>
		public static readonly BindableProperty OrgRoleProperty =
			BindableProperty.Create(nameof(OrgRole), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The organization role
		/// </summary>
		public string OrgRole
		{
			get => (string)this.GetValue(OrgRoleProperty);
			set => this.SetValue(OrgRoleProperty, value);
		}

		/// <summary>
		/// The <see cref="DeviceId"/>
		/// </summary>
		public static readonly BindableProperty DeviceIdProperty =
			BindableProperty.Create(nameof(DeviceId), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The device id.
		/// </summary>
		public string DeviceId
		{
			get => (string)this.GetValue(DeviceIdProperty);
			set => this.SetValue(DeviceIdProperty, value);
		}

		/// <summary>
		/// The user's legal identity, set when the registration has occurred.
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the app is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get => (bool)this.GetValue(IsConnectedProperty);
			set => this.SetValue(IsConnectedProperty, value);
		}

		/// <summary>
		/// The <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user friendly connection state text to display to the user.
		/// </summary>
		public string ConnectionStateText
		{
			get => (string)this.GetValue(ConnectionStateTextProperty);
			set => this.SetValue(ConnectionStateTextProperty, value);
		}

		#endregion

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.RegisterCommand.ChangeCanExecute();
			});

			return Task.CompletedTask;
		}

		private static void OnPropertyChanged(BindableObject b, object oldValue, object newValue)
		{
			RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
			viewModel.RegisterCommand.ChangeCanExecute();
		}

		private void SetConnectionStateAndText(XmppState state)
		{
			this.IsConnected = state == XmppState.Connected;
			this.ConnectionStateText = state.ToDisplayText();
		}

		private async Task TakePhoto()
		{
			if (!this.XmppService.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["ServerDoesNotSupportFileUpload"]);
				return;
			}

			if (Device.RuntimePlatform == Device.iOS)
			{
				MediaFile CapturedPhoto;

				try
				{
					CapturedPhoto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
					{
						CompressionQuality = 80,
						RotateImage = false
					});
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["TakingAPhotoIsNotSupported"] + ": " + ex.Message);
					return;
				}

				if (CapturedPhoto is not null)
				{
					try
					{
						await this.AddPhoto(CapturedPhoto.Path, true);
					}
					catch (Exception ex)
					{
						await this.UiSerializer.DisplayAlert(ex);
					}
				}
			}
			else
			{
				FileResult CapturedPhoto;

				try
				{
					CapturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (CapturedPhoto is null)
						return;
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["TakingAPhotoIsNotSupported"] + ": " + ex.Message);
					return;
				}

				if (CapturedPhoto is not null)
				{
					try
					{
						await this.AddPhoto(CapturedPhoto.FullPath, true);
					}
					catch (Exception ex)
					{
						await this.UiSerializer.DisplayAlert(ex);
					}
				}
			}
		}

		private async Task PickPhoto()
		{
			if (!this.XmppService.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PickPhoto"], LocalizationResourceManager.Current["SelectingAPhotoIsNotSupported"]);
				return;
			}

			FileResult PickedPhoto = await MediaPicker.PickPhotoAsync();

			if (PickedPhoto is not null)
				await this.AddPhoto(PickedPhoto.FullPath, true);
		}

		private async Task ScanPassport()
		{
			// TODO: Open Camera View with preview, constantly scanning for MRZ codes.

			string FileName;

			if (Device.RuntimePlatform == Device.iOS)
			{
				MediaFile CapturedPhoto;

				try
				{
					CapturedPhoto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
					{
						CompressionQuality = 80,
						RotateImage = false
					});
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["TakingAPhotoIsNotSupported"] + ": " + ex.Message);
					return;
				}

				if (CapturedPhoto is null)
					return;

				FileName = CapturedPhoto.Path;
			}
			else
			{
				FileResult CapturedPhoto;

				try
				{
					CapturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (CapturedPhoto is null)
						return;
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["TakingAPhotoIsNotSupported"] + ": " + ex.Message);
					return;
				}

				FileName = CapturedPhoto.FullPath;
			}

			try
			{
				IMatrix M = Bitmaps.FromBitmapFile(FileName, 600, 600);

				if (EXIF.TryExtractFromJPeg(FileName, out ExifTag[] Tags))
				{
					switch (PhotosLoader.GetImageRotation(Tags))
					{
						case -90:
							M = M.Rotate270();
							break;

						case 90:
							M = M.Rotate90();
							break;

						case 180:
							M = M.Rotate180();
							break;
					}
				}

				Matrix<int> Grayscale = (Matrix<int>)M.GrayScaleFixed();
				Matrix<int> Mrz = Grayscale.ExtractMrzRegion();

				if (Mrz is null)
					return;

				IOcrService OcrService = App.Instantiate<IOcrService>();

				Mrz.Negate();
				Mrz.Contrast();
				string[] Rows = await OcrService.ProcessImage(Mrz, "mrz", PageSegmentationMode.SingleUniformBlockOfText);

				if (Rows.Length == 0)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToOcrImage"]);
					return;
				}

				DocumentInformation DocInfo = null;
				int c = Rows.Length;

				if (c >= 3)
				{
					if (!BasicAccessControl.ParseMrz(Rows[c - 3] + "\n" + Rows[c - 2] + "\n" + Rows[c - 1], out DocInfo))
						DocInfo = null;
				}

				if (DocInfo is null && c >= 2)
				{
					if (!BasicAccessControl.ParseMrz(Rows[c - 2] + "\n" + Rows[c - 1], out DocInfo))
						DocInfo = null;
				}

				if (DocInfo is null)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToExtractMachineReadableString"]);
					return;
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
			finally
			{
				File.Delete(FileName);
			}
		}

		/// <summary>
		/// Adds a photo from the specified path to use as a profile photo.
		/// </summary>
		/// <param name="Bin">Binary content</param>
		/// <param name="ContentType">Content-Type</param>
		/// <param name="Rotation">Rotation to use, to display the image correctly.</param>
		/// <param name="saveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		/// <param name="showAlert">Set to <c>true</c> to show an alert if photo is too large; <c>false</c> otherwise.</param>
		protected internal async Task AddPhoto(byte[] Bin, string ContentType, int Rotation, bool saveLocalCopy, bool showAlert)
		{
			if (Bin.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
			{
				if (showAlert)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PhotoIsTooLarge"]);

				return;
			}

			this.RemovePhoto(saveLocalCopy);

			if (saveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(this.localPhotoFileName, Bin);
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}
			}

			this.photo = new LegalIdentityAttachment(this.localPhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));

			this.RegisterCommand.ChangeCanExecute();
		}

		/// <summary>
		/// Adds a photo from the specified path to use as a profile photo.
		/// </summary>
		/// <param name="FilePath">The full path to the file.</param>
		/// <param name="SaveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		protected internal async Task AddPhoto(string FilePath, bool SaveLocalCopy)
		{
			try
			{
				bool FallbackOriginal = true;

				if (SaveLocalCopy)
				{
					// try to downscale and comress the image
					using FileStream InputStream = File.OpenRead(FilePath);
					using SKData ImageData = this.CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await this.AddPhoto(ImageData.ToArray(), Constants.MimeTypes.Jpeg, 0, SaveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(FilePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
						ContentType = "application/octet-stream";

					await this.AddPhoto(Bin, ContentType, PhotosLoader.GetImageRotation(Bin), SaveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToLoadPhoto"]);
				this.LogService.LogException(ex);
				return;
			}
		}

		private SKData CompressImage(Stream inputStream)
		{
			try
			{
				using SKManagedStream ManagedStream = new(inputStream);
				using SKData ImageData = SKData.Create(ManagedStream);

				SKCodec Codec = SKCodec.Create(ImageData);
				SKBitmap SkBitmap = SKBitmap.Decode(ImageData);

				SkBitmap = this.HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// downdsample to FHD
				if ((Width >= Height) && (Width > 1920))
				{
					Height = (int)(Height * (1920.0 / Width) + 0.5);
					Width = 1920;
					Resize = true;
				}
				else if ((Height > Width) && (Height > 1920))
				{
					Width = (int)(Width * (1920.0 / Height) + 0.5);
					Height = 1920;
					Resize = true;
				}

				if (Resize)
				{
					SKImageInfo Info = SkBitmap.Info;
					SKImageInfo NewInfo = new(Width, Height, Info.ColorType, Info.AlphaType, Info.ColorSpace);
					SkBitmap = SkBitmap.Resize(NewInfo, SKFilterQuality.High);
				}

				return SkBitmap.Encode(SKEncodedImageFormat.Jpeg, 80);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}

			return null;
		}

		private SKBitmap HandleOrientation(SKBitmap Bitmap, SKEncodedOrigin Orientation)
		{
			SKBitmap Rotated;

			switch (Orientation)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(Bitmap.Width, Bitmap.Height);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.RotateDegrees(180, Bitmap.Width / 2, Bitmap.Height / 2);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(Rotated.Width, 0);
						Surface.RotateDegrees(90);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(0, Rotated.Height);
						Surface.RotateDegrees(270);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				default:
					return Bitmap;
			}

			return Rotated;
		}

		private void RemovePhoto(bool RemoveFileOnDisc)
		{
			try
			{
				this.photo = null;
				this.Image = null;

				if (RemoveFileOnDisc && File.Exists(this.localPhotoFileName))
					File.Delete(this.localPhotoFileName);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		private async Task Register()
		{
			if (!(await this.ValidateInput(true)))
				return;

			string CountryCode = ISO_3166_1.ToCode(this.SelectedCountry);
			string PnrBeforeValidation = this.PersonalNumber.Trim();
			NumberInformation NumberInfo = await PersonalNumberSchemes.Validate(CountryCode, PnrBeforeValidation);

			if (NumberInfo.IsValid.HasValue && !NumberInfo.IsValid.Value)
			{
				if (string.IsNullOrWhiteSpace(NumberInfo.DisplayString))
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PersonalNumberDoesNotMatchCountry"]);
				else
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PersonalNumberDoesNotMatchCountry_ExpectedFormat"] + NumberInfo.DisplayString);

				return;
			}

			if (NumberInfo.PersonalNumber != PnrBeforeValidation)
				this.PersonalNumber = NumberInfo.PersonalNumber;

			if (string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["OperatorDoesNotSupportLegalIdentitiesAndSmartContracts"]);
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TagProfile.RegistryJid))
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["OperatorDoesNotSupportThingRegistries"]);
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TagProfile.ProvisioningJid))
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["OperatorDoesNotSupportProvisioningAndDecisionSupportForThings"]);
				return;
			}

			if (!this.XmppService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NotConnectedToTheOperator"]);
				return;
			}

			this.SetIsBusy(this.RegisterCommand, this.TakePhotoCommand, this.PickPhotoCommand, this.EPassportCommand);

			try
			{
				RegisterIdentityModel IdentityModel = this.CreateRegisterModel();
				LegalIdentityAttachment[] Photos = { this.photo };

				(bool Succeeded, LegalIdentity AddedIdentity) = await this.NetworkService.TryRequest(() =>
					this.XmppService.AddLegalIdentity(IdentityModel, Photos));

				if (Succeeded)
				{
					this.LegalIdentity = AddedIdentity;
					await this.TagProfile.SetLegalIdentity(this.LegalIdentity);
					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						this.SetIsDone(this.RegisterCommand, this.TakePhotoCommand, this.PickPhotoCommand, this.EPassportCommand);
						this.OnStepCompleted(EventArgs.Empty);
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.RegisterCommand, this.TakePhotoCommand, this.PickPhotoCommand, this.EPassportCommand);
			}
		}

		private bool CanRegister()
		{
			// Ok to 'wait' on, since we're not actually waiting on anything.
			return this.ValidateInput(false).GetAwaiter().GetResult() && this.XmppService.IsOnline;
		}

		private RegisterIdentityModel CreateRegisterModel()
		{
			RegisterIdentityModel IdentityModel = new();
			string s;

			if (!string.IsNullOrWhiteSpace(s = this.FirstName?.Trim()))
				IdentityModel.FirstName = s;

			if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
				IdentityModel.MiddleNames = s;

			if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
				IdentityModel.LastNames = s;

			if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
				IdentityModel.PersonalNumber = s;

			if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
				IdentityModel.Address = s;

			if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
				IdentityModel.Address2 = s;

			if (!string.IsNullOrWhiteSpace(s = this.ZipCode?.Trim()))
				IdentityModel.ZipCode = s;

			if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
				IdentityModel.Area = s;

			if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
				IdentityModel.City = s;

			if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
				IdentityModel.Region = s;

			if (!string.IsNullOrWhiteSpace(s = this.SelectedCountry?.Trim()))
				IdentityModel.Country = s;

			if (this.ShowOrganization)
			{
				if (!string.IsNullOrWhiteSpace(s = this.OrgName?.Trim()))
					IdentityModel.OrgName = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgNumber?.Trim()))
					IdentityModel.OrgNumber = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgDepartment?.Trim()))
					IdentityModel.OrgDepartment = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgRole?.Trim()))
					IdentityModel.OrgRole = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgAddress?.Trim()))
					IdentityModel.OrgAddress = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgAddress2?.Trim()))
					IdentityModel.OrgAddress2 = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgZipCode?.Trim()))
					IdentityModel.OrgZipCode = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgArea?.Trim()))
					IdentityModel.OrgArea = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgCity?.Trim()))
					IdentityModel.OrgCity = s;

				if (!string.IsNullOrWhiteSpace(s = this.OrgRegion?.Trim()))
					IdentityModel.OrgRegion = s;

				if (!string.IsNullOrWhiteSpace(s = this.SelectedOrgCountry?.Trim()))
					IdentityModel.OrgCountry = s;
			}

			if (!string.IsNullOrWhiteSpace(s = this.TagProfile?.PhoneNumber?.Trim()))
			{
				if (string.IsNullOrWhiteSpace(s) && this.TagProfile.LegalIdentity is not null)
					s = this.TagProfile.LegalIdentity[Constants.XmppProperties.Phone];

				IdentityModel.PhoneNr = s;
			}

			if (!string.IsNullOrWhiteSpace(s = this.TagProfile?.EMail?.Trim()))
			{
				if (string.IsNullOrWhiteSpace(s) && this.TagProfile.LegalIdentity is not null)
					s = this.TagProfile.LegalIdentity[Constants.XmppProperties.EMail];

				IdentityModel.EMail = s;
			}

			return IdentityModel;
		}

		private async Task<bool> ValidateInput(bool AlertUser)
		{
			if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
			{
				if (AlertUser)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
						LocalizationResourceManager.Current["YouNeedToProvideAFirstName"]);
				}

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
			{
				if (AlertUser)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
						LocalizationResourceManager.Current["YouNeedToProvideALastName"]);
				}

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.PersonalNumber?.Trim()))
			{
				if (AlertUser)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
						LocalizationResourceManager.Current["YouNeedToProvideAPersonalNumber"]);
				}

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.SelectedCountry))
			{
				if (AlertUser)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
						LocalizationResourceManager.Current["YouNeedToProvideACountry"]);
				}

				return false;
			}

			if (this.photo is null)
			{
				if (AlertUser)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
						LocalizationResourceManager.Current["YouNeedToProvideAPhoto"]);
				}

				return false;
			}

			if (this.Purpose == PurposeUse.Work)
			{
				if (string.IsNullOrWhiteSpace(this.OrgName?.Trim()))
				{
					if (AlertUser)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
							LocalizationResourceManager.Current["YouNeedToProvideAnOrgName"]);
					}

					return false;
				}

				if (string.IsNullOrWhiteSpace(this.OrgNumber?.Trim()))
				{
					if (AlertUser)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
							LocalizationResourceManager.Current["YouNeedToProvideAnOrgNumber"]);
					}

					return false;
				}

				if (string.IsNullOrWhiteSpace(this.OrgDepartment?.Trim()))
				{
					if (AlertUser)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
							LocalizationResourceManager.Current["YouNeedToProvideAnOrgDepartment"]);
					}

					return false;
				}

				if (string.IsNullOrWhiteSpace(this.OrgRole?.Trim()))
				{
					if (AlertUser)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
							LocalizationResourceManager.Current["YouNeedToProvideAnOrgRole"]);
					}

					return false;
				}

				if (string.IsNullOrWhiteSpace(this.SelectedOrgCountry))
				{
					if (AlertUser)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"],
							LocalizationResourceManager.Current["YouNeedToProvideAnOrgCountry"]);
					}

					return false;
				}
			}

			return true;
		}

		/// <inheritdoc />
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();

			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedCountry)), this.SelectedCountry);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.FirstName)), this.FirstName);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.MiddleNames)), this.MiddleNames);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.LastNames)), this.LastNames);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.PersonalNumber)), this.PersonalNumber);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Address)), this.Address);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Address2)), this.Address2);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Area)), this.Area);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.City)), this.City);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.ZipCode)), this.ZipCode);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Region)), this.Region);

			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedOrgCountry)), this.SelectedOrgCountry);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgName)), this.OrgName);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgDepartment)), this.OrgDepartment);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgRole)), this.OrgRole);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgNumber)), this.OrgNumber);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgAddress)), this.OrgAddress);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgAddress2)), this.OrgAddress2);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgArea)), this.OrgArea);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgCity)), this.OrgCity);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgZipCode)), this.OrgZipCode);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.OrgRegion)), this.OrgRegion);
		}

		/// <inheritdoc />
		protected override async Task DoRestoreState()
		{
			this.SelectedCountry = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.SelectedCountry)));
			this.FirstName = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.FirstName)));
			this.MiddleNames = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.MiddleNames)));
			this.LastNames = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.LastNames)));
			this.PersonalNumber = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.PersonalNumber)));
			this.Address = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Address)));
			this.Address2 = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Address2)));
			this.Area = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Area)));
			this.City = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.City)));
			this.ZipCode = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.ZipCode)));
			this.Region = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Region)));

			this.SelectedOrgCountry = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.SelectedOrgCountry)));
			this.OrgName = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgName)));
			this.OrgNumber = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgNumber)));
			this.OrgDepartment = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgDepartment)));
			this.OrgRole = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgRole)));
			this.OrgAddress = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgAddress)));
			this.OrgAddress2 = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgAddress2)));
			this.OrgArea = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgArea)));
			this.OrgCity = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgCity)));
			this.OrgZipCode = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgZipCode)));
			this.OrgRegion = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.OrgRegion)));

			try
			{
				if (this.TagProfile.Step > RegistrationStep.Account && File.Exists(this.localPhotoFileName))
					await this.AddPhoto(this.localPhotoFileName, false);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}

			await base.DoRestoreState();
		}

		/// <inheritdoc />
		public override void ClearStepState()
		{
			this.RemovePhoto(true);

			this.SelectedCountry = null;
			this.FirstName = string.Empty;
			this.MiddleNames = string.Empty;
			this.LastNames = string.Empty;
			this.PersonalNumber = string.Empty;
			this.Address = string.Empty;
			this.Address2 = string.Empty;
			this.Area = string.Empty;
			this.City = string.Empty;
			this.ZipCode = string.Empty;
			this.Region = string.Empty;

			this.SelectedOrgCountry = null;
			this.OrgName = string.Empty;
			this.OrgNumber = string.Empty;
			this.OrgDepartment = string.Empty;
			this.OrgRole = string.Empty;
			this.OrgAddress = string.Empty;
			this.OrgAddress2 = string.Empty;
			this.OrgArea = string.Empty;
			this.OrgCity = string.Empty;
			this.OrgZipCode = string.Empty;
			this.OrgRegion = string.Empty;

			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedCountry)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.FirstName)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.MiddleNames)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.LastNames)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.PersonalNumber)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Address)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Address2)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Area)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.City)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.ZipCode)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Region)));

			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedOrgCountry)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgName)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgNumber)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgDepartment)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgRole)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgAddress)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgAddress2)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgArea)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgCity)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgZipCode)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.OrgRegion)));
		}

		/// <summary>
		/// Copies values from the existing TAG Profile's Legal identity.
		/// </summary>
		public virtual void PopulateFromTagProfile()
		{
			LegalIdentity Identity = this.TagProfile.LegalIdentity;

			if (Identity is not null)
			{
				this.FirstName = Identity[Constants.XmppProperties.FirstName];
				this.MiddleNames = Identity[Constants.XmppProperties.MiddleName];
				this.LastNames = Identity[Constants.XmppProperties.LastName];
				this.PersonalNumber = Identity[Constants.XmppProperties.PersonalNumber];
				this.Address = Identity[Constants.XmppProperties.Address];
				this.Address2 = Identity[Constants.XmppProperties.Address2];
				this.ZipCode = Identity[Constants.XmppProperties.ZipCode];
				this.Area = Identity[Constants.XmppProperties.Area];
				this.City = Identity[Constants.XmppProperties.City];
				this.Region = Identity[Constants.XmppProperties.Region];
				string CountryCode = Identity[Constants.XmppProperties.Country];

				if (!string.IsNullOrWhiteSpace(CountryCode) && ISO_3166_1.TryGetCountry(CountryCode, out string Country))
					this.SelectedCountry = Country;

				this.OrgName = Identity[Constants.XmppProperties.OrgName];
				this.OrgNumber = Identity[Constants.XmppProperties.OrgNumber];
				this.OrgDepartment = Identity[Constants.XmppProperties.OrgDepartment];
				this.OrgRole = Identity[Constants.XmppProperties.OrgRole];
				this.OrgAddress = Identity[Constants.XmppProperties.OrgAddress];
				this.OrgAddress2 = Identity[Constants.XmppProperties.OrgAddress2];
				this.OrgZipCode = Identity[Constants.XmppProperties.OrgZipCode];
				this.OrgArea = Identity[Constants.XmppProperties.OrgArea];
				this.OrgCity = Identity[Constants.XmppProperties.OrgCity];
				this.OrgRegion = Identity[Constants.XmppProperties.OrgRegion];
				string OrgCountryCode = Identity[Constants.XmppProperties.OrgCountry];

				if (!string.IsNullOrWhiteSpace(OrgCountryCode) && ISO_3166_1.TryGetCountry(OrgCountryCode, out string OrgCountry))
					this.SelectedOrgCountry = OrgCountry;

				Attachment FirstAttachment = Identity.Attachments?.GetFirstImageAttachment();
				if (FirstAttachment is not null)
				{
					// Don't await this one, just let it run asynchronously.
					this.photosLoader
						.LoadOnePhoto(FirstAttachment, SignWith.LatestApprovedIdOrCurrentKeys)
						.ContinueWith(task =>
						{
							(byte[] Bin, string ContentType, int Rotation) = task.Result;
							if (Bin is not null)
							{
								if (!this.IsAppearing) // Page no longer on screen when download is done?
									return;

								this.UiSerializer.BeginInvokeOnMainThread(async () =>
								{
									await this.AddPhoto(Bin, ContentType, Rotation, true, false);
								});
							}
						}, TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnCanceled);
				}
			}
		}
	}
}
