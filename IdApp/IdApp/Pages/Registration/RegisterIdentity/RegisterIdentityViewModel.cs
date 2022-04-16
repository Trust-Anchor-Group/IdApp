﻿using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Linear;
using IdApp.Cv.Utilities;
using IdApp.DeviceSpecific;
using IdApp.Extensions;
using IdApp.Services.Data.PersonalNumbers;
using IdApp.Services.Contracts;
using IdApp.Services.Xmpp;
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
using IdApp.Resx;
using SkiaSharp;

namespace IdApp.Pages.Registration.RegisterIdentity
{
	/// <summary>
	/// The view model to bind to when showing Step 3 of the registration flow: registering an identity.
	/// </summary>
	public class RegisterIdentityViewModel : RegistrationStepViewModel
	{
		private const string ProfilePhotoFileName = "ProfilePhoto.jpg";
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
			this.RegisterCommand = new Command(async _ => await Register(), _ => CanRegister());
			this.TakePhotoCommand = new Command(async _ => await TakePhoto(), _ => !IsBusy);
			this.PickPhotoCommand = new Command(async _ => await PickPhoto(), _ => !IsBusy);
			this.EPassportCommand = new Command(async _ => await ScanPassport(), _ => !IsBusy);
			this.RemovePhotoCommand = new Command(_ => RemovePhoto(true));

			this.Title = AppResources.PersonalLegalInformation;
			this.PersonalNumberPlaceholder = AppResources.PersonalNumber;

			this.localPhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProfilePhotoFileName);
			this.photosLoader = new PhotosLoader();
		}

		/// <inheritdoc />
		protected override async Task DoBind()
		{
			await base.DoBind();
			RegisterCommand.ChangeCanExecute();
			this.XmppService.ConnectionStateChanged += XmppService_ConnectionStateChanged;
		}

		/// <inheritdoc />
		protected override async Task DoUnbind()
		{
			this.photosLoader.CancelLoadPhotos();
			this.XmppService.ConnectionStateChanged -= XmppService_ConnectionStateChanged;
			await base.DoUnbind();
		}

		#region Properties

		/// <summary>
		/// True if the user choose the educational or experimental purpose.
		/// </summary>
		public bool IsTest => this.TagProfile.IsTest;

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
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty HasPhotoProperty =
			BindableProperty.Create("HasPhoto", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the user has selected a photo for their account or not.
		/// </summary>
		public bool HasPhoto
		{
			get { return (bool)GetValue(HasPhotoProperty); }
			set { SetValue(HasPhotoProperty, value); }
		}

		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty ImageProperty =
			BindableProperty.Create("Image", typeof(ImageSource), typeof(RegisterIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
				viewModel.HasPhoto = !(newValue is null);
			});

		/// <summary>
		/// The image source, i.e. the file representing the selected photo.
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
			BindableProperty.Create("ImageRotation", typeof(int), typeof(Main.Main.MainViewModel), default(int));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public int ImageRotation
		{
			get { return (int)GetValue(ImageRotationProperty); }
			set { SetValue(ImageRotationProperty, value); }
		}

		/// <summary>
		/// The list of all available countries a user can select from.
		/// </summary>
		public ObservableCollection<string> Countries { get; }

		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty SelectedCountryProperty =
			BindableProperty.Create("SelectedCountry", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
				viewModel.RegisterCommand.ChangeCanExecute();
				if (!string.IsNullOrWhiteSpace(viewModel.SelectedCountry) &&
					ISO_3166_1.TryGetCode(viewModel.SelectedCountry, out string CountryCode))
				{
					string format = PersonalNumberSchemes.DisplayStringForCountry(CountryCode);
					if (!string.IsNullOrWhiteSpace(format))
						viewModel.PersonalNumberPlaceholder = string.Format(AppResources.PersonalNumberPlaceholder, format);
					else
						viewModel.PersonalNumberPlaceholder = AppResources.PersonalNumber;
				}
				else
					viewModel.PersonalNumberPlaceholder = AppResources.PersonalNumber;
			});

		/// <summary>
		/// The user selected country from the list of <see cref="Countries"/>.
		/// </summary>
		public string SelectedCountry
		{
			get { return (string)GetValue(SelectedCountryProperty); }
			set { SetValue(SelectedCountryProperty, value); }
		}

		/// <summary>
		/// The <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create("FirstName", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's first name
		/// </summary>
		public string FirstName
		{
			get { return (string)GetValue(FirstNameProperty); }
			set { SetValue(FirstNameProperty, value); }
		}

		/// <summary>
		/// The <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create("MiddleNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's middle name(s)
		/// </summary>
		public string MiddleNames
		{
			get { return (string)GetValue(MiddleNamesProperty); }
			set { SetValue(MiddleNamesProperty, value); }
		}

		/// <summary>
		/// The <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create("LastNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's last name(s)
		/// </summary>
		public string LastNames
		{
			get { return (string)GetValue(LastNamesProperty); }
			set { SetValue(LastNamesProperty, value); }
		}

		/// <summary>
		/// The <see cref="PersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberProperty =
			BindableProperty.Create("PersonalNumber", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's personal number
		/// </summary>
		public string PersonalNumber
		{
			get { return (string)GetValue(PersonalNumberProperty); }
			set { SetValue(PersonalNumberProperty, value); }
		}

		/// <summary>
		/// The <see cref="PersonalNumberPlaceholder"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberPlaceholderProperty =
			BindableProperty.Create("PersonalNumberPlaceholder", typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The personal number placeholder, used as a guide to the user to enter the correct format, which depends on the <see cref="SelectedCountry"/>.
		/// </summary>
		public string PersonalNumberPlaceholder
		{
			get { return (string)GetValue(PersonalNumberPlaceholderProperty); }
			set { SetValue(PersonalNumberPlaceholderProperty, value); }
		}

		/// <summary>
		/// The <see cref="Address"/>
		/// </summary>
		public static readonly BindableProperty AddressProperty =
			BindableProperty.Create("Address", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's address, line 1.
		/// </summary>
		public string Address
		{
			get { return (string)GetValue(AddressProperty); }
			set { SetValue(AddressProperty, value); }
		}

		/// <summary>
		/// The <see cref="Address2"/>
		/// </summary>
		public static readonly BindableProperty Address2Property =
			BindableProperty.Create("Address2", typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's address, line 2.
		/// </summary>
		public string Address2
		{
			get { return (string)GetValue(Address2Property); }
			set { SetValue(Address2Property, value); }
		}

		/// <summary>
		/// The <see cref="ZipCode"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeProperty =
			BindableProperty.Create("ZipCode", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's zip code
		/// </summary>
		public string ZipCode
		{
			get { return (string)GetValue(ZipCodeProperty); }
			set { SetValue(ZipCodeProperty, value); }
		}

		/// <summary>
		/// The <see cref="Area"/>
		/// </summary>
		public static readonly BindableProperty AreaProperty =
			BindableProperty.Create("Area", typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's area
		/// </summary>
		public string Area
		{
			get { return (string)GetValue(AreaProperty); }
			set { SetValue(AreaProperty, value); }
		}

		/// <summary>
		/// The <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create("City", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's city
		/// </summary>
		public string City
		{
			get { return (string)GetValue(CityProperty); }
			set { SetValue(CityProperty, value); }
		}

		/// <summary>
		/// The <see cref="Region"/>
		/// </summary>
		public static readonly BindableProperty RegionProperty =
			BindableProperty.Create("Region", typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's region
		/// </summary>
		public string Region
		{
			get { return (string)GetValue(RegionProperty); }
			set { SetValue(RegionProperty, value); }
		}

		/// <summary>
		/// The <see cref="DeviceId"/>
		/// </summary>
		public static readonly BindableProperty DeviceIdProperty =
			BindableProperty.Create("DeviceId", typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The device id.
		/// </summary>
		public string DeviceId
		{
			get { return (string)GetValue(DeviceIdProperty); }
			set { SetValue(DeviceIdProperty, value); }
		}

		/// <summary>
		/// The user's legal identity, set when the registration has occurred.
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create("IsConnected", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the app is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}

		/// <summary>
		/// The <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create("ConnectionStateText", typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user friendly connection state text to display to the user.
		/// </summary>
		public string ConnectionStateText
		{
			get { return (string)GetValue(ConnectionStateTextProperty); }
			set { SetValue(ConnectionStateTextProperty, value); }
		}

		#endregion

		private void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				SetConnectionStateAndText(e.State);
				RegisterCommand.ChangeCanExecute();
			});
		}

		private static void OnPropertyChanged(BindableObject b, object oldValue, object newValue)
		{
			RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
			viewModel.RegisterCommand.ChangeCanExecute();
		}

		private void SetConnectionStateAndText(XmppState state)
		{
			IsConnected = state == XmppState.Connected;
			this.ConnectionStateText = state.ToDisplayText();
		}

		private async Task TakePhoto()
		{
			if (!this.XmppService.Contracts.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.ServerDoesNotSupportFileUpload);
				return;
			}

			if (Device.RuntimePlatform == Device.iOS)
			{
				MediaFile capturedPhoto;

				try
				{
					capturedPhoto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
					{
						CompressionQuality = 80,
						RotateImage = false
					});
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported + ": " + ex.Message);
					return;
				}

				if (!(capturedPhoto is null))
				{
					try
					{
						await AddPhoto(capturedPhoto.Path, true);
					}
					catch (Exception ex)
					{
						await this.UiSerializer.DisplayAlert(ex);
					}
				}
			}
			else
			{
				FileResult capturedPhoto;

				try
				{
					capturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (capturedPhoto is null)
						return;
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported + ": " + ex.Message);
					return;
				}

				if (!(capturedPhoto is null))
				{
					try
					{
						await AddPhoto(capturedPhoto.FullPath, true);
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
			if (!this.XmppService.Contracts.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(AppResources.PickPhoto, AppResources.SelectingAPhotoIsNotSupported);
				return;
			}

			FileResult pickedPhoto = await MediaPicker.PickPhotoAsync();

			if (!(pickedPhoto is null))
				await AddPhoto(pickedPhoto.FullPath, true);
		}

		private async Task ScanPassport()
		{
			// TODO: Open Camera View with preview, constantly scanning for MRZ codes.

			string FileName;

			if (Device.RuntimePlatform == Device.iOS)
			{
				MediaFile capturedPhoto;

				try
				{
					capturedPhoto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
					{
						CompressionQuality = 80,
						RotateImage = false
					});
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported + ": " + ex.Message);
					return;
				}

				if (capturedPhoto is null)
					return;

				FileName = capturedPhoto.Path;
			}
			else
			{
				FileResult capturedPhoto;

				try
				{
					capturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (capturedPhoto is null)
						return;
				}
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported + ": " + ex.Message);
					return;
				}

				FileName = capturedPhoto.FullPath;
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
				if (!OcrService.Created)
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.TesseractNotCreated);
					return;
				}

				if (!OcrService.Initialized)
				{
					if (!await OcrService.Initialize())
					{
						await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnabletoInitializeTesseract);
						return;
					}
				}

				Mrz.Negate();
				Mrz.Contrast();
				string s = Convert.ToBase64String(Bitmaps.EncodeAsPng(Mrz));  // TODO: Remove
				string[] Rows = await OcrService.ProcessImage(Mrz);

				if (Rows.Length == 0)
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToTesseractImage);
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
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToExtractMachineReadableString);
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
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PhotoIsTooLarge);

				return;
			}

			RemovePhoto(saveLocalCopy);

			if (saveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(localPhotoFileName, Bin);
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}
			}

			this.photo = new LegalIdentityAttachment(localPhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));

			RegisterCommand.ChangeCanExecute();
		}

		/// <summary>
		/// Adds a photo from the specified path to use as a profile photo.
		/// </summary>
		/// <param name="filePath">The full path to the file.</param>
		/// <param name="saveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		protected internal async Task AddPhoto(string filePath, bool saveLocalCopy)
		{
			try
			{
				bool FallbackOriginal = true;

				if (saveLocalCopy)
				{
					// try to downscale and comress the image
					using FileStream InputStream = File.OpenRead(filePath);
					using SKData ImageData = CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await AddPhoto(ImageData.ToArray(), "image/jpeg", 0, saveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{ 
					byte[] Bin = File.ReadAllBytes(filePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(filePath), out string ContentType))
						ContentType = "application/octet-stream";

					await AddPhoto(Bin, ContentType, PhotosLoader.GetImageRotation(Bin), saveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToLoadPhoto);
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

				SkBitmap = HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// resize if more than 4K
				if ((Width >= Height) && (Width > 3840))
				{
					Height = (int)(Height * (3840.0 / Width) + 0.5);
					Width = 3840;
					Resize = true;
				}
				else if ((Height > Width) && (Height > 3840))
				{
					Width = (int)(Width * (3840.0 / Height) + 0.5);
					Height = 3840;
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
			catch (Exception)
			{
			}

			return null;
		}

		private SKBitmap HandleOrientation(SKBitmap bitmap, SKEncodedOrigin orientation)
		{
			SKBitmap Rotated;

			switch (orientation)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(bitmap.Width, bitmap.Height);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.RotateDegrees(180, bitmap.Width / 2, bitmap.Height / 2);
						Surface.DrawBitmap(bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(bitmap.Height, bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(Rotated.Width, 0);
						Surface.RotateDegrees(90);
						Surface.DrawBitmap(bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(bitmap.Height, bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(0, Rotated.Height);
						Surface.RotateDegrees(270);
						Surface.DrawBitmap(bitmap, 0, 0);
					}
					break;

				default:
					return bitmap;
			}

			return Rotated;
		}

		private void RemovePhoto(bool removeFileOnDisc)
		{
			try
			{
				this.photo = null;
				Image = null;

				if (removeFileOnDisc && File.Exists(this.localPhotoFileName))
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

			string countryCode = ISO_3166_1.ToCode(this.SelectedCountry);
			string pnrBeforeValidation = PersonalNumber.Trim();
			NumberInformation NumberInfo = await PersonalNumberSchemes.Validate(countryCode, pnrBeforeValidation);

			if (NumberInfo.IsValid.HasValue && !NumberInfo.IsValid.Value)
			{
				if (string.IsNullOrWhiteSpace(NumberInfo.DisplayString))
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PersonalNumberDoesNotMatchCountry);
				else
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PersonalNumberDoesNotMatchCountry_ExpectedFormat + NumberInfo.DisplayString);

				return;
			}

			if (NumberInfo.PersonalNumber != pnrBeforeValidation)
				this.PersonalNumber = NumberInfo.PersonalNumber;

			if (string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.OperatorDoesNotSupportLegalIdentitiesAndSmartContracts);
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TagProfile.RegistryJid))
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.OperatorDoesNotSupportThingRegistries);
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TagProfile.ProvisioningJid))
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.OperatorDoesNotSupportProvisioningAndDecisionSupportForThings);
				return;
			}

			if (!this.XmppService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NotConnectedToTheOperator);
				return;
			}

			SetIsBusy(RegisterCommand, TakePhotoCommand, PickPhotoCommand, EPassportCommand);

			try
			{
				RegisterIdentityModel model = CreateRegisterModel();
				LegalIdentityAttachment[] photos = { this.photo };
				(bool succeeded, LegalIdentity addedIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.AddLegalIdentity(model, photos));
				if (succeeded)
				{
					this.LegalIdentity = addedIdentity;
					this.TagProfile.SetLegalIdentity(this.LegalIdentity);
					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						SetIsDone(RegisterCommand, TakePhotoCommand, PickPhotoCommand, EPassportCommand);
						OnStepCompleted(EventArgs.Empty);
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
				BeginInvokeSetIsDone(RegisterCommand, TakePhotoCommand, PickPhotoCommand, EPassportCommand);
			}
		}

		private bool CanRegister()
		{
			// Ok to 'wait' on, since we're not actually waiting on anything.
			return ValidateInput(false).GetAwaiter().GetResult() && this.XmppService.IsOnline;
		}

		private RegisterIdentityModel CreateRegisterModel()
		{
			string s;
			RegisterIdentityModel model = new();
			if (!string.IsNullOrWhiteSpace(s = this.FirstName?.Trim()))
				model.FirstName = s;

			if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
				model.MiddleNames = s;

			if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
				model.LastNames = s;

			if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
				model.PersonalNumber = s;

			if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
				model.Address = s;

			if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
				model.Address2 = s;

			if (!string.IsNullOrWhiteSpace(s = this.ZipCode?.Trim()))
				model.ZipCode = s;

			if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
				model.Area = s;

			if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
				model.City = s;

			if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
				model.Region = s;

			if (!string.IsNullOrWhiteSpace(s = this.SelectedCountry?.Trim()))
				model.Country = s;

			if (!string.IsNullOrWhiteSpace(s = this.TagProfile?.PhoneNumber?.Trim()))
				model.PhoneNr = s;

			return model;
		}

		private async Task<bool> ValidateInput(bool alertUser)
		{
			if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
			{
				if (alertUser)
					await this.UiSerializer.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideAFirstName);

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
			{
				if (alertUser)
					await this.UiSerializer.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideALastName);

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.PersonalNumber?.Trim()))
			{
				if (alertUser)
					await this.UiSerializer.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideAPersonalNumber);

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.SelectedCountry))
			{
				if (alertUser)
					await this.UiSerializer.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideACountry);

				return false;
			}

			if (this.photo is null && alertUser)
			{
				await this.UiSerializer.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideAPhoto);

				return false;
			}

			return true;
		}

		/// <inheritdoc />
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();
			await this.SettingsService.SaveState(GetSettingsKey(nameof(SelectedCountry)), this.SelectedCountry);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(FirstName)), this.FirstName);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(MiddleNames)), this.MiddleNames);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(LastNames)), this.LastNames);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(PersonalNumber)), this.PersonalNumber);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(Address)), this.Address);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(Address2)), this.Address2);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(Area)), this.Area);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(City)), this.City);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(ZipCode)), this.ZipCode);
			await this.SettingsService.SaveState(GetSettingsKey(nameof(Region)), this.Region);
		}

		/// <inheritdoc />
		protected override async Task DoRestoreState()
		{
			this.SelectedCountry = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(SelectedCountry)));
			this.FirstName = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(FirstName)));
			this.MiddleNames = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(MiddleNames)));
			this.LastNames = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(LastNames)));
			this.PersonalNumber = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(PersonalNumber)));
			this.Address = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Address)));
			this.Address2 = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Address2)));
			this.Area = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Area)));
			this.City = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(City)));
			this.ZipCode = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(ZipCode)));
			this.Region = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Region)));

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
			RemovePhoto(true);

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

			this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedCountry)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(FirstName)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(MiddleNames)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(LastNames)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(PersonalNumber)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(Address)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(Address2)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(Area)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(City)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(ZipCode)));
			this.SettingsService.RemoveState(GetSettingsKey(nameof(Region)));
		}

		/// <summary>
		/// Copies values from the existing TAG Profile's Legal identity.
		/// </summary>
		public virtual void PopulateFromTagProfile()
		{
			LegalIdentity identity = this.TagProfile.LegalIdentity;
			if (!(identity is null))
			{
				this.FirstName = identity[Constants.XmppProperties.FirstName];
				this.MiddleNames = identity[Constants.XmppProperties.MiddleName];
				this.LastNames = identity[Constants.XmppProperties.LastName];
				this.PersonalNumber = identity[Constants.XmppProperties.PersonalNumber];
				this.Address = identity[Constants.XmppProperties.Address];
				this.Address2 = identity[Constants.XmppProperties.Address2];
				this.ZipCode = identity[Constants.XmppProperties.ZipCode];
				this.Area = identity[Constants.XmppProperties.Area];
				this.City = identity[Constants.XmppProperties.City];
				this.Region = identity[Constants.XmppProperties.Region];
				string CountryCode = identity[Constants.XmppProperties.Country];
				string PhoneNr = this.TagProfile.PhoneNumber ?? identity[Constants.XmppProperties.Phone];

				if (!string.IsNullOrWhiteSpace(CountryCode) && ISO_3166_1.TryGetCountry(CountryCode, out string Country))
					this.SelectedCountry = Country;

				Attachment FirstAttachment = identity.Attachments?.GetFirstImageAttachment();
				if (!(FirstAttachment is null))
				{
					// Don't await this one, just let it run asynchronously.
					this.photosLoader
						.LoadOnePhoto(FirstAttachment, SignWith.LatestApprovedIdOrCurrentKeys)
						.ContinueWith(task =>
						{
							(byte[] Bin, string ContentType, int Rotation) = task.Result;
							if (!(Bin is null))
							{
								if (!this.IsBound) // Page no longer on screen when download is done?
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