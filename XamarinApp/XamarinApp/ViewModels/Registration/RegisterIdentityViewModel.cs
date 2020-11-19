using System;
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
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegisterIdentityViewModel : RegistrationStepViewModel
    {
        private readonly Dictionary<string, LegalIdentityAttachment> photos;

        public RegisterIdentityViewModel(RegistrationStep step, TagProfile tagProfile, ITagService tagService, IMessageService messageService)
         : base(step, tagProfile, tagService, messageService)
        {
            IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();
            this.DeviceId = deviceInfo?.GetDeviceID();
            this.Countries = new ObservableCollection<string>();
            foreach (string country in ISO_3166_1.Countries)
                this.Countries.Add(country);
            this.SelectedCountry = null;
            this.RegisterCommand = new Command(async _ => await Register(), _ => CanRegister());
            this.TakePhotoCommand = new Command(async _ => await TakePhoto());
            photos = new Dictionary<string, LegalIdentityAttachment>();

            // TODO: set this property "OnAppearing" (how to do that for views?) -  Move property to parent vm?
            CanTakePhoto = CrossMedia.IsSupported &&
                CrossMedia.Current.IsCameraAvailable &&
                CrossMedia.Current.IsTakePhotoSupported &&
                this.TagService.FileUploadIsSupported;
        }

        public ICommand RegisterCommand { get; }

        public ICommand TakePhotoCommand { get; }

        public ObservableCollection<string> Countries { get; }

        public static readonly BindableProperty SelectedCountryProperty =
            BindableProperty.Create("SelectedCountry", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string SelectedCountry
        {
            get { return (string)GetValue(SelectedCountryProperty); }
            set { SetValue(SelectedCountryProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(RegisterIdentityViewModel), default(string));

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
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
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

        public static readonly BindableProperty PostalCodeProperty =
            BindableProperty.Create("PostalCode", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string PostalCode
        {
            get { return (string)GetValue(PostalCodeProperty); }
            set { SetValue(PostalCodeProperty, value); }
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

        public static readonly BindableProperty CanTakePhotoProperty =
            BindableProperty.Create("CanTakePhoto", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

        public bool CanTakePhoto
        {
            get { return (bool)GetValue(CanTakePhotoProperty); }
            set { SetValue(CanTakePhotoProperty, value); }
        }

        public LegalIdentity LegalIdentity { get; private set; }

        private async Task TakePhoto()
        {
            MediaFile photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
            {
                MaxWidthHeight = 1024,
                CompressionQuality = 100,
                AllowCropping = true,
                ModalPresentationStyle = MediaPickerModalPresentationStyle.FullScreen,
                RotateImage = true,
                SaveMetaData = true,
                Directory = "Photos",
                Name = "Photo.jpg",
                DefaultCamera = CameraDevice.Rear
            });

            if (photo is null)
                return;

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (Stream f = photo.GetStream())
                {
                    f.CopyTo(ms);
                }
                ms.Reset();
                bytes = ms.ToArray();
            }

            if (bytes.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
            {
                await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.PhotoIsTooLarge);
                return;
            }

            string photoId = Guid.NewGuid().ToString();
            this.photos[photoId] = new LegalIdentityAttachment(photo.Path, Constants.MimeTypes.Jpeg, bytes);
        }

        private async Task Register()
        {
            if (!this.TagService.IsOnline)
            {
                await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.NotConnectedToTheOperator);
                return;
            }

            if (string.IsNullOrEmpty(this.TagProfile.LegalJid))
            {
                await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.OperatorDoesNotSupportLegalIdentitiesAndSmartContracts);
                return;
            }

            IsBusy = true;
            RegisterCommand.ChangeCanExecute();

            try
            {
                this.LegalIdentity = await this.TagService.AddLegalIdentityAsync(GetProperties(), this.photos.Values.ToArray());
                Device.BeginInvokeOnMainThread(() =>
                {
                    SetIsDone(RegisterCommand);
                    OnStepCompleted(EventArgs.Empty);
                });
            }
            catch (Exception ex)
            {
                await this.MessageService.DisplayAlert(ex);
            }
            finally
            {
                BeginInvokeSetIsDone(RegisterCommand);
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
                properties.Add(new Property("FIRST", s));

            if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
                properties.Add(new Property("MIDDLE", s));

            if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
                properties.Add(new Property("LAST", s));

            if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
                properties.Add(new Property("PNR", s));

            if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
                properties.Add(new Property("ADDR", s));

            if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
                properties.Add(new Property("ADDR2", s));

            if (!string.IsNullOrWhiteSpace(s = this.PostalCode?.Trim()))
                properties.Add(new Property("ZIP", s));

            if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
                properties.Add(new Property("AREA", s));

            if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
                properties.Add(new Property("CITY", s));

            if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
                properties.Add(new Property("REGION", s));

            if (!string.IsNullOrWhiteSpace(s = this.SelectedCountry?.Trim()))
                properties.Add(new Property("COUNTRY", ISO_3166_1.ToCode(s)));

            if (!string.IsNullOrWhiteSpace(s = this.DeviceId?.Trim()))
                properties.Add(new Property("DEVICE_ID", s));

            properties.Add(new Property("JID", this.TagService.BareJId));

            return properties;
        }

        private async Task<bool> ValidateInput(bool alertUser)
        {
            if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
            {
                if (alertUser)
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAFirstName);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
            {
                if (alertUser)
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideALastName);
                }
                return false;
            }

            if (string.IsNullOrEmpty(this.PersonalNumber?.Trim()))
            {
                if (alertUser)
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAPersonalNumber);
                }
                return false;
            }

            return true;
        }
    }
}