﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.PersonalNumbers;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegisterIdentityViewModel : RegistrationStepViewModel
    {
        private readonly Dictionary<string, LegalIdentityAttachment> photos;
        private readonly IContractsService contractsService;

        public RegisterIdentityViewModel(
            TagProfile tagProfile, 
            INeuronService neuronService, 
            INavigationService navigationService, 
            IContractsService contractsService)
         : base(RegistrationStep.RegisterIdentity, tagProfile, neuronService, navigationService)
        {
            this.contractsService = contractsService;
            IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();
            this.DeviceId = deviceInfo?.GetDeviceID();
            this.Countries = new ObservableCollection<string>();
            foreach (string country in ISO_3166_1.Countries)
                this.Countries.Add(country);
            this.SelectedCountry = null;
            this.RegisterCommand = new Command(async _ => await Register(), _ => CanRegister());
            this.TakePhotoCommand = new Command(async _ => await TakePhoto(), _ => !IsBusy);
            this.PickPhotoCommand = new Command(async _ => await PickPhoto(), _ => !IsBusy);
            this.RemovePhotoCommand = new Command(_ => Image = null);
            this.photos = new Dictionary<string, LegalIdentityAttachment>();
            this.Title = AppResources.PersonalLegalInformation;
            this.PersonalNumberPlaceholder = AppResources.PersonalNumber;
        }

        public ICommand RegisterCommand { get; }
        public ICommand TakePhotoCommand { get; }
        public ICommand PickPhotoCommand { get; }
        public ICommand RemovePhotoCommand { get; }

        public static readonly BindableProperty HasPhotoProperty =
            BindableProperty.Create("HasPhoto", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

        public bool HasPhoto
        {
            get { return (bool) GetValue(HasPhotoProperty); }
            set { SetValue(HasPhotoProperty, value); }
        }

        public static readonly BindableProperty ImageProperty =
            BindableProperty.Create("Image", typeof(ImageSource), typeof(RegisterIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.HasPhoto = newValue != null;
            });

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public ObservableCollection<string> Countries { get; }

        public static readonly BindableProperty SelectedCountryProperty =
            BindableProperty.Create("SelectedCountry", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.RegisterCommand.ChangeCanExecute();
                string code;
                if (!string.IsNullOrWhiteSpace(viewModel.SelectedCountry) && ISO_3166_1.TryGetCode(viewModel.SelectedCountry, out code))
                {
                    string format = PersonalNumberSchemes.DisplayStringForCountry(code);
                    if (!string.IsNullOrWhiteSpace(format))
                    {
                        viewModel.PersonalNumberPlaceholder = string.Format(AppResources.PersonalNumberPlaceholder, format);
                    }
                    else
                    {
                        viewModel.PersonalNumberPlaceholder = AppResources.PersonalNumber;
                    }
                }
                else
                {
                    viewModel.PersonalNumberPlaceholder = AppResources.PersonalNumber;
                }
            });

        public string SelectedCountry
        {
            get { return (string)GetValue(SelectedCountryProperty); }
            set { SetValue(SelectedCountryProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.RegisterCommand.ChangeCanExecute();
            });

        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.RegisterCommand.ChangeCanExecute();
            });

        public string MiddleNames
        {
            get { return (string)GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string LastNames
        {
            get { return (string)GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.RegisterCommand.ChangeCanExecute();
            });

        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberPlaceholderProperty =
            BindableProperty.Create("PersonalNumberPlaceholder", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string PersonalNumberPlaceholder
        {
            get { return (string) GetValue(PersonalNumberPlaceholderProperty); }
            set { SetValue(PersonalNumberPlaceholderProperty, value); }
        }

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Address2
        {
            get { return (string)GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string ZipCode
        {
            get { return (string)GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Area
        {
            get { return (string)GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public static readonly BindableProperty DeviceIdProperty =
            BindableProperty.Create("DeviceId", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string DeviceId
        {
            get { return (string)GetValue(DeviceIdProperty); }
            set { SetValue(DeviceIdProperty, value); }
        }

        public LegalIdentity LegalIdentity { get; private set; }

        private async Task TakePhoto()
        {
            if (!(CrossMedia.IsSupported &&
                CrossMedia.Current.IsCameraAvailable &&
                CrossMedia.Current.IsTakePhotoSupported &&
                this.contractsService.FileUploadIsSupported))
            {
                await this.NavigationService.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported);
                return;
            }

            MediaFile photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                MaxWidthHeight = 1024,
                CompressionQuality = 100,
                AllowCropping = true,
                ModalPresentationStyle = MediaPickerModalPresentationStyle.FullScreen,
                RotateImage = true,
                SaveMetaData = true,
                Directory = "Photos",
                Name = $"Photo_{DateTime.UtcNow.Ticks}.jpg",
                DefaultCamera = CameraDevice.Front
            });

            if (photo is null)
                return;

            await AddPhoto(photo);
        }

        private async Task PickPhoto()
        {
            if (!(CrossMedia.IsSupported &&
                  CrossMedia.Current.IsPickPhotoSupported &&
                  this.contractsService.FileUploadIsSupported))
            {
                await this.NavigationService.DisplayAlert(AppResources.PickPhoto, AppResources.SelectingAPhotoIsNotSupported);
                return;
            }

            MediaFile photo = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions
            {
                MaxWidthHeight = 1024,
                CompressionQuality = 100,
                ModalPresentationStyle = MediaPickerModalPresentationStyle.FullScreen,
                RotateImage = true,
                SaveMetaData = true,
            });

            if (photo is null)
                return;

            await AddPhoto(photo);
        }

        private async Task AddPhoto(MediaFile photo)
        {
            MemoryStream ms = new MemoryStream();
            using (Stream f = photo.GetStream())
            {
                f.CopyTo(ms);
            }
            ms.Reset();
            byte[] bytes = ms.ToArray();

            if (bytes.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
            {
                ms.Dispose();
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PhotoIsTooLarge);
                return;
            }

            string photoId = Guid.NewGuid().ToString();
            this.photos[photoId] = new LegalIdentityAttachment(photo.Path, Constants.MimeTypes.Jpeg, bytes);
            ms.Reset();
            Image = ImageSource.FromStream(() => ms); // .FromStream disposes the stream
        }

        private async Task Register()
        {
            if (!(await this.ValidateInput(true)))
            {
                return;
            }

            string countryCode = ISO_3166_1.ToCode(this.SelectedCountry);
            bool? personalNumberIsValid = PersonalNumberSchemes.IsValid(countryCode, this.PersonalNumber, out string personalNumberFormat);

            if (personalNumberIsValid.HasValue && !personalNumberIsValid.Value)
            {
                if (string.IsNullOrEmpty(personalNumberFormat))
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PersonalNumberDoesNotMatchCountry);
                else
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PersonalNumberDoesNotMatchCountry_ExpectedFormat + personalNumberFormat);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
            {
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.OperatorDoesNotSupportLegalIdentitiesAndSmartContracts);
                return;
            }

            if (!this.NeuronService.IsOnline)
            {
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.NotConnectedToTheOperator);
                return;
            }

            SetIsBusy(RegisterCommand, TakePhotoCommand, PickPhotoCommand);

            try
            {
                this.LegalIdentity = await this.contractsService.AddLegalIdentityAsync(GetProperties(), this.photos.Values.ToArray());
                Device.BeginInvokeOnMainThread(() =>
                {
                    SetIsDone(RegisterCommand, TakePhotoCommand, PickPhotoCommand);
                    OnStepCompleted(EventArgs.Empty);
                });
            }
            catch (Exception ex)
            {
                await this.NavigationService.DisplayAlert(ex);
            }
            finally
            {
                BeginInvokeSetIsDone(RegisterCommand, TakePhotoCommand, PickPhotoCommand);
            }
        }

        private bool CanRegister()
        {
            // Ok to 'wait' on, since we're not actually waiting on anything.
            return ValidateInput(false).GetAwaiter().GetResult();
        }

        private List<Property> GetProperties()
        {
            List<Property> properties = new List<Property>();
            string s;

            if (!string.IsNullOrWhiteSpace(s = this.FirstName?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.FirstName, s));

            if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.MiddleName, s));

            if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.LastName, s));

            if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.PersonalNumber, s));

            if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Address, s));

            if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Address2, s));

            if (!string.IsNullOrWhiteSpace(s = this.ZipCode?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.ZipCode, s));

            if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Area, s));

            if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.City, s));

            if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Region, s));

            if (!string.IsNullOrWhiteSpace(s = this.SelectedCountry?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Country, ISO_3166_1.ToCode(s)));

            if (!string.IsNullOrWhiteSpace(s = this.DeviceId?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.DeviceId, s));

            properties.Add(new Property(Constants.XmppProperties.JId, this.NeuronService.BareJId));

            return properties;
        }

        private async Task<bool> ValidateInput(bool alertUser)
        {
            if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAFirstName);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideALastName);
                }
                return false;
            }

            if (string.IsNullOrEmpty(this.PersonalNumber?.Trim()))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAPersonalNumber);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.SelectedCountry))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideACountry);
                }
                return false;
            }

            return true;
        }

        protected override async Task DoSaveState()
        {
            await base.DoSaveState();
            // TODO: save properties
        }

        protected override async Task DoRestoreState()
        {
            // TODO: restore properties
            await base.DoRestoreState();
        }
    }
}