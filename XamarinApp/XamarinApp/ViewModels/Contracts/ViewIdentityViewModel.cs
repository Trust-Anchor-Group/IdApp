using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Extensions;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Views.Registration;

namespace XamarinApp.ViewModels.Contracts
{
    public class ViewIdentityViewModel : BaseViewModel
    {
        private const string IdToReview = "IdToReview";
        private SignaturePetitionEventArgs identityToReview;
        private readonly TagProfile tagProfile;
        private readonly IUiDispatcher uiDispatcher;
        private readonly ILogService logService;
        private readonly INeuronService neuronService;
        private readonly ISettingsService settingsService;
        private readonly INavigationService navigationService;
        private readonly INetworkService networkService;
        private readonly PhotosLoader photosLoader;
        private bool persistState;

        public ViewIdentityViewModel(
            LegalIdentity identity,
            SignaturePetitionEventArgs identityToReview,
            TagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService,
            ISettingsService settingsService,
            INavigationService navigationService,
            INetworkService networkService,
            ILogService logService)
        {
            this.LegalIdentity = identity ?? tagProfile.LegalIdentity;
            this.identityToReview = identityToReview;
            this.tagProfile = tagProfile;
            this.uiDispatcher = uiDispatcher;
            this.logService = logService;
            this.neuronService = neuronService;
            this.settingsService = settingsService;
            this.navigationService = navigationService;
            this.networkService = networkService;
            this.ApproveCommand = new Command(async _ => await Approve());
            this.RejectCommand = new Command(async _ => await Reject());
            this.RevokeCommand = new Command(async _ => await Revoke());
            this.CompromiseCommand = new Command(async _ => await Compromise());
            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.neuronService, this.Photos);
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            string idToReviewAsJson = this.settingsService.RestoreState<string>(GetSettingsKey(IdToReview));
            if ( !string.IsNullOrWhiteSpace(idToReviewAsJson))
            {
                try
                {
                    this.identityToReview = JsonConvert.DeserializeObject<SignaturePetitionEventArgs>(idToReviewAsJson);
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex);
                    this.identityToReview = null;
                }
            }
            AssignProperties();
            this.tagProfile.Changed += TagProfile_Changed;
            this.neuronService.Contracts.LegalIdentityChanged += NeuronContracts_LegalIdentityChanged;
            this.persistState = true;
        }

        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            this.tagProfile.Changed -= TagProfile_Changed;
            this.neuronService.Contracts.LegalIdentityChanged -= NeuronContracts_LegalIdentityChanged;
            if (this.persistState && this.identityToReview != null)
            {
                this.settingsService.SaveState(GetSettingsKey(IdToReview), JsonConvert.SerializeObject(identityToReview));
            }
            else
            {
                this.settingsService.RemoveState<string>(GetSettingsKey(IdToReview));
            }
            await base.DoUnbind();
        }

        public ObservableCollection<ImageSource> Photos { get; }

        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand CompromiseCommand { get; }
        public ICommand RevokeCommand { get; }

        private void AssignProperties()
        {
            Created = this.LegalIdentity?.Created ?? DateTime.MinValue;
            Updated = this.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
            LegalId = this.LegalIdentity?.Id;
            BareJId = this.neuronService?.BareJId ?? string.Empty;
            if (this.LegalIdentity != null)
            {
                PublicKey = Convert.ToBase64String(this.LegalIdentity.ClientPubKey);
            }
            else
            {
                PublicKey = string.Empty;
            }
            State = this.LegalIdentity?.State ?? IdentityState.Rejected;
            From = this.LegalIdentity?.From.GetDateOrNullIfMinValue();
            To = this.LegalIdentity?.To.GetDateOrNullIfMinValue();
            if (this.LegalIdentity != null)
            {
                FirstName = this.LegalIdentity[Constants.XmppProperties.FirstName];
                MiddleNames = this.LegalIdentity[Constants.XmppProperties.MiddleName];
                LastNames = this.LegalIdentity[Constants.XmppProperties.LastName];
                PersonalNumber = this.LegalIdentity[Constants.XmppProperties.PersonalNumber];
                Address = this.LegalIdentity[Constants.XmppProperties.Address];
                Address2 = this.LegalIdentity[Constants.XmppProperties.Address2];
                ZipCode = this.LegalIdentity[Constants.XmppProperties.ZipCode];
                Area = this.LegalIdentity[Constants.XmppProperties.Area];
                City = this.LegalIdentity[Constants.XmppProperties.City];
                Region = this.LegalIdentity[Constants.XmppProperties.Region];
                CountryCode = this.LegalIdentity[Constants.XmppProperties.Country];
            }
            else
            {
                FirstName = string.Empty;
                MiddleNames = string.Empty;
                LastNames = string.Empty;
                PersonalNumber = string.Empty;
                Address = string.Empty;
                Address2 = string.Empty;
                ZipCode = string.Empty;
                Area = string.Empty;
                City = string.Empty;
                Region = string.Empty;
                CountryCode = string.Empty;
            }
            Country = ISO_3166_1.ToName(this.CountryCode);
            IsApproved = this.LegalIdentity?.State == IdentityState.Approved;
            IsCreated = this.LegalIdentity?.State == IdentityState.Created;

            IsPersonal = this.tagProfile.LegalIdentity?.Id == this.LegalIdentity?.Id;
            IsForReview = this.identityToReview != null;
            IsNotForReview = !IsForReview;

            IsForReviewFirstName = !string.IsNullOrWhiteSpace(this.FirstName) && this.IsForReview;
            IsForReviewMiddleNames = !string.IsNullOrWhiteSpace(this.MiddleNames) && this.IsForReview;
            IsForReviewLastNames = !string.IsNullOrWhiteSpace(this.LastNames) && this.IsForReview;
            IsForReviewPersonalNumber = !string.IsNullOrWhiteSpace(this.PersonalNumber) && this.IsForReview;
            IsForReviewAddress = !string.IsNullOrWhiteSpace(this.Address) && this.IsForReview;
            IsForReviewAddress2 = !string.IsNullOrWhiteSpace(this.Address2) && this.IsForReview;
            IsForReviewCity = !string.IsNullOrWhiteSpace(this.City) && this.IsForReview;
            IsForReviewZipCode = !string.IsNullOrWhiteSpace(this.ZipCode) && this.IsForReview;
            IsForReviewArea = !string.IsNullOrWhiteSpace(this.Area) && this.IsForReview;
            IsForReviewRegion = !string.IsNullOrWhiteSpace(this.Region) && this.IsForReview;
            IsForReviewCountry = !string.IsNullOrWhiteSpace(this.Country) && this.IsForReview;
            IsForReviewAndPin = this.identityToReview != null && this.tagProfile.UsePin;

            // QR
            if (this.LegalIdentity != null)
            {
                _ = Task.Run(() =>
                {
                    byte[] png = QR.GenerateCodePng(Constants.IoTSchemes.CreateIdUri(this.LegalIdentity.Id), this.QrCodeWidth, this.QrCodeHeight);
                    this.uiDispatcher.BeginInvokeOnMainThread(() => this.QrCode = ImageSource.FromStream(() => new MemoryStream(png)));
                });
            }
            else
            {
                this.QrCode = null;
            }

            this.photosLoader.CancelLoadPhotos();
            if (this.tagProfile?.LegalIdentity?.Attachments != null)
            {
                _ = this.photosLoader.LoadPhotos(this.tagProfile.LegalIdentity.Attachments);
            }
        }

        private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(AssignProperties);
        }

        private void NeuronContracts_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.LegalIdentity = e.Identity;
                AssignProperties();
            });
        }

        #region Properties

        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create("Created", typeof(DateTime), typeof(ViewIdentityViewModel), default(DateTime));

        public DateTime Created
        {
            get { return (DateTime)GetValue(CreatedProperty); }
            set { SetValue(CreatedProperty, value); }
        }

        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create("Updated", typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

        public DateTime? Updated
        {
            get { return (DateTime?)GetValue(UpdatedProperty); }
            set { SetValue(UpdatedProperty, value); }
        }

        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string LegalId
        {
            get { return (string)GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        public LegalIdentity LegalIdentity { get; private set; }

        public static readonly BindableProperty BareJIdProperty =
            BindableProperty.Create("BareJId", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string BareJId
        {
            get { return (string)GetValue(BareJIdProperty); }
            set { SetValue(BareJIdProperty, value); }
        }

        public static readonly BindableProperty PublicKeyProperty =
            BindableProperty.Create("PublicKey", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string PublicKey
        {
            get { return (string) GetValue(PublicKeyProperty); }
            set { SetValue(PublicKeyProperty, value); }
        }

        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(IdentityState), typeof(ViewIdentityViewModel), default(IdentityState));

        public IdentityState State
        {
            get { return (IdentityState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

        public DateTime? From
        {
            get { return (DateTime?)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

        public DateTime? To
        {
            get { return (DateTime?)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string MiddleNames
        {
            get { return (string)GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string LastNames
        {
            get { return (string)GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string Address2
        {
            get { return (string)GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string ZipCode
        {
            get { return (string)GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string Area
        {
            get { return (string)GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string CountryCode
        {
            get { return (string)GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public static readonly BindableProperty QrCodeProperty =
            BindableProperty.Create("QrCode", typeof(ImageSource), typeof(ViewIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                ViewIdentityViewModel viewModel = (ViewIdentityViewModel)b;
                viewModel.HasQrCode = newValue != null;
            });

        public ImageSource QrCode
        {
            get { return (ImageSource)GetValue(QrCodeProperty); }
            set { SetValue(QrCodeProperty, value); }
        }

        public static readonly BindableProperty HasQrCodeProperty =
            BindableProperty.Create("HasQrCode", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool HasQrCode
        {
            get { return (bool)GetValue(HasQrCodeProperty); }
            set { SetValue(HasQrCodeProperty, value); }
        }

        public static readonly BindableProperty QrCodeWidthProperty =
            BindableProperty.Create("QrCodeWidth", typeof(int), typeof(ViewIdentityViewModel), 350);

        public int QrCodeWidth
        {
            get { return (int)GetValue(QrCodeWidthProperty); }
            set { SetValue(QrCodeWidthProperty, value); }
        }

        public static readonly BindableProperty QrCodeHeightProperty =
            BindableProperty.Create("QrCodeHeight", typeof(int), typeof(ViewIdentityViewModel), 350);

        public int QrCodeHeight
        {
            get { return (int)GetValue(QrCodeHeightProperty); }
            set { SetValue(QrCodeHeightProperty, value); }
        }

        public bool IsApproved
        {
            get { return (bool)GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        public static readonly BindableProperty IsCreatedProperty =
            BindableProperty.Create("IsCreated", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsCreated
        {
            get { return (bool)GetValue(IsCreatedProperty); }
            set { SetValue(IsCreatedProperty, value); }
        }

        public static readonly BindableProperty IsForReviewProperty =
            BindableProperty.Create("IsForReview", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReview
        {
            get { return (bool)GetValue(IsForReviewProperty); }
            set { SetValue(IsForReviewProperty, value); }
        }

        public static readonly BindableProperty IsNotForReviewProperty =
            BindableProperty.Create("IsNotForReview", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsNotForReview
        {
            get { return (bool) GetValue(IsNotForReviewProperty); }
            set { SetValue(IsNotForReviewProperty, value); }
        }

        public static readonly BindableProperty IsPersonalProperty =
            BindableProperty.Create("IsPersonal", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsPersonal
        {
            get { return (bool)GetValue(IsPersonalProperty); }
            set { SetValue(IsPersonalProperty, value); }
        }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(ViewIdentityViewModel), default(string));

        public string Pin
        {
            get { return (string) GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        public static readonly BindableProperty FirstNameIsCheckedProperty =
            BindableProperty.Create("FirstNameIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool FirstNameIsChecked
        {
            get { return (bool)GetValue(FirstNameIsCheckedProperty); }
            set { SetValue(FirstNameIsCheckedProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesIsCheckedProperty =
            BindableProperty.Create("MiddleNamesIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool MiddleNamesIsChecked
        {
            get { return (bool)GetValue(MiddleNamesIsCheckedProperty); }
            set { SetValue(MiddleNamesIsCheckedProperty, value); }
        }

        public static readonly BindableProperty LastNamesIsCheckedProperty =
            BindableProperty.Create("LastNamesIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool LastNamesIsChecked
        {
            get { return (bool)GetValue(LastNamesIsCheckedProperty); }
            set { SetValue(LastNamesIsCheckedProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberIsCheckedProperty =
            BindableProperty.Create("PersonalNumberIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool PersonalNumberIsChecked
        {
            get { return (bool)GetValue(PersonalNumberIsCheckedProperty); }
            set { SetValue(PersonalNumberIsCheckedProperty, value); }
        }

        public static readonly BindableProperty AddressIsCheckedProperty =
            BindableProperty.Create("AddressIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool AddressIsChecked
        {
            get { return (bool)GetValue(AddressIsCheckedProperty); }
            set { SetValue(AddressIsCheckedProperty, value); }
        }

        public static readonly BindableProperty Address2IsCheckedProperty =
            BindableProperty.Create("Address2IsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool Address2IsChecked
        {
            get { return (bool)GetValue(Address2IsCheckedProperty); }
            set { SetValue(Address2IsCheckedProperty, value); }
        }

        public static readonly BindableProperty ZipCodeIsCheckedProperty =
            BindableProperty.Create("ZipCodeIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool ZipCodeIsChecked
        {
            get { return (bool)GetValue(ZipCodeIsCheckedProperty); }
            set { SetValue(ZipCodeIsCheckedProperty, value); }
        }

        public static readonly BindableProperty AreaIsCheckedProperty =
            BindableProperty.Create("AreaIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool AreaIsChecked
        {
            get { return (bool)GetValue(AreaIsCheckedProperty); }
            set { SetValue(AreaIsCheckedProperty, value); }
        }

        public static readonly BindableProperty CityIsCheckedProperty =
            BindableProperty.Create("CityIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool CityIsChecked
        {
            get { return (bool)GetValue(CityIsCheckedProperty); }
            set { SetValue(CityIsCheckedProperty, value); }
        }

        public static readonly BindableProperty RegionIsCheckedProperty =
            BindableProperty.Create("RegionIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool RegionIsChecked
        {
            get { return (bool)GetValue(RegionIsCheckedProperty); }
            set { SetValue(RegionIsCheckedProperty, value); }
        }

        public static readonly BindableProperty CountryCodeIsCheckedProperty =
            BindableProperty.Create("CountryCodeIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool CountryCodeIsChecked
        {
            get { return (bool)GetValue(CountryCodeIsCheckedProperty); }
            set { SetValue(CountryCodeIsCheckedProperty, value); }
        }

        public static readonly BindableProperty CarefulReviewIsCheckedProperty =
            BindableProperty.Create("CarefulReviewIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool CarefulReviewIsChecked
        {
            get { return (bool)GetValue(CarefulReviewIsCheckedProperty); }
            set { SetValue(CarefulReviewIsCheckedProperty, value); }
        }

        public static readonly BindableProperty ApprovePiiIsCheckedProperty =
            BindableProperty.Create("ApprovePiiIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool ApprovePiiIsChecked
        {
            get { return (bool)GetValue(ApprovePiiIsCheckedProperty); }
            set { SetValue(ApprovePiiIsCheckedProperty, value); }
        }

        public static readonly BindableProperty IsForReviewFirstNameProperty =
            BindableProperty.Create("IsForReviewFirstName", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewFirstName
        {
            get { return (bool) GetValue(IsForReviewFirstNameProperty); }
            set { SetValue(IsForReviewFirstNameProperty, value); }
        }

        public static readonly BindableProperty IsForReviewMiddleNamesProperty =
            BindableProperty.Create("IsForReviewMiddleNames", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewMiddleNames
        {
            get { return (bool) GetValue(IsForReviewMiddleNamesProperty); }
            set { SetValue(IsForReviewMiddleNamesProperty, value); }
        }

        public static readonly BindableProperty IsForReviewLastNamesProperty =
            BindableProperty.Create("IsForReviewLastNames", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewLastNames
        {
            get { return (bool) GetValue(IsForReviewLastNamesProperty); }
            set { SetValue(IsForReviewLastNamesProperty, value); }
        }

        public static readonly BindableProperty IsForReviewPersonalNumberProperty =
            BindableProperty.Create("IsForReviewPersonalNumber", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewPersonalNumber
        {
            get { return (bool) GetValue(IsForReviewPersonalNumberProperty); }
            set { SetValue(IsForReviewPersonalNumberProperty, value); }
        }

        public static readonly BindableProperty IsForReviewAddressProperty =
            BindableProperty.Create("IsForReviewAddress", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewAddress
        {
            get { return (bool) GetValue(IsForReviewAddressProperty); }
            set { SetValue(IsForReviewAddressProperty, value); }
        }

        public static readonly BindableProperty IsForReviewAddress2Property =
            BindableProperty.Create("IsForReviewAddress2", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewAddress2
        {
            get { return (bool) GetValue(IsForReviewAddress2Property); }
            set { SetValue(IsForReviewAddress2Property, value); }
        }

        public static readonly BindableProperty IsForReviewCityProperty =
            BindableProperty.Create("IsForReviewCity", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewCity
        {
            get { return (bool) GetValue(IsForReviewCityProperty); }
            set { SetValue(IsForReviewCityProperty, value); }
        }

        public static readonly BindableProperty IsForReviewZipCodeProperty =
            BindableProperty.Create("IsForReviewZipCode", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewZipCode
        {
            get { return (bool) GetValue(IsForReviewZipCodeProperty); }
            set { SetValue(IsForReviewZipCodeProperty, value); }
        }

        public static readonly BindableProperty IsForReviewAreaProperty =
            BindableProperty.Create("IsForReviewArea", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewArea
        {
            get { return (bool) GetValue(IsForReviewAreaProperty); }
            set { SetValue(IsForReviewAreaProperty, value); }
        }
        
        public static readonly BindableProperty IsForReviewRegionProperty =
            BindableProperty.Create("IsForReviewRegion", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewRegion
        {
            get { return (bool) GetValue(IsForReviewRegionProperty); }
            set { SetValue(IsForReviewRegionProperty, value); }
        }

        public static readonly BindableProperty IsForReviewCountryProperty =
            BindableProperty.Create("IsForReviewCountry", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewCountry
        {
            get { return (bool) GetValue(IsForReviewCountryProperty); }
            set { SetValue(IsForReviewCountryProperty, value); }
        }


        public static readonly BindableProperty IsForReviewAndPinProperty =
            BindableProperty.Create("IsForReviewAndPin", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

        public bool IsForReviewAndPin
        {
            get { return (bool)GetValue(IsForReviewAndPinProperty); }
            set { SetValue(IsForReviewAndPinProperty, value); }
        }

        #endregion

        private async Task Approve()
        {
            if (this.identityToReview is null)
                return;

            try
            {
                if ((!string.IsNullOrEmpty(this.FirstName) && !this.FirstNameIsChecked) ||
                    (!string.IsNullOrEmpty(this.MiddleNames) && !this.MiddleNamesIsChecked) ||
                    (!string.IsNullOrEmpty(this.LastNames) && !this.LastNamesIsChecked) ||
                    (!string.IsNullOrEmpty(this.PersonalNumber) && !this.PersonalNumberIsChecked) ||
                    (!string.IsNullOrEmpty(this.Address) && !this.AddressIsChecked) ||
                    (!string.IsNullOrEmpty(this.Address2) && !this.Address2IsChecked) ||
                    (!string.IsNullOrEmpty(this.ZipCode) && !this.ZipCodeIsChecked) ||
                    (!string.IsNullOrEmpty(this.Area) && !this.AreaIsChecked) ||
                    (!string.IsNullOrEmpty(this.City) && !this.CityIsChecked) ||
                    (!string.IsNullOrEmpty(this.Region) && !this.RegionIsChecked) ||
                    (!string.IsNullOrEmpty(this.CountryCode) && !this.CountryCodeIsChecked))
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.Incomplete, AppResources.PleaseReviewAndCheckAllCheckboxes);
                    return;
                }

                if (!this.CarefulReviewIsChecked)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.Incomplete, AppResources.YouNeedToCheckCarefullyReviewed);
                    return;
                }

                if (!this.ApprovePiiIsChecked)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.Incomplete, AppResources.YouNeedToApproveToAssociate);
                    return;
                }

                if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
                    return;
                }

                (bool succeeded1, byte[] signature) = await this.networkService.Request(this.neuronService.Contracts.SignAsync, this.identityToReview.ContentToSign);

                if (!succeeded1)
                {
                    return;
                }

                bool succeeded2 = await this.networkService.Request(this.neuronService.Contracts.SendPetitionSignatureResponseAsync, this.identityToReview.SignatoryIdentityId, this.identityToReview.ContentToSign, signature, this.identityToReview.PetitionId, this.identityToReview.RequestorFullJid, true);

                if (succeeded2)
                {
                    this.persistState = false;
                    await this.navigationService.PopAsync();
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task Reject()
        {
            if (this.identityToReview == null)
                return;

            try
            {
                bool succeeded = await this.networkService.Request(this.neuronService.Contracts.SendPetitionSignatureResponseAsync, this.identityToReview.SignatoryIdentityId, this.identityToReview.ContentToSign, new byte[0], this.identityToReview.PetitionId, this.identityToReview.RequestorFullJid, false);
                if (succeeded)
                {
                    this.persistState = false;
                    await this.navigationService.PopAsync();
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task Revoke()
        {
            if (!this.IsPersonal)
                return;

            try
            {
                if (!await this.uiDispatcher.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.Cancel))
                    return;

                (bool succeeded, LegalIdentity revokedIdentity) = await this.networkService.Request(this.neuronService.Contracts.ObsoleteLegalIdentityAsync, this.LegalIdentity.Id);
                if (succeeded)
                {
                    this.LegalIdentity = revokedIdentity;
                    this.tagProfile.RevokeLegalIdentity(revokedIdentity);
                    await this.navigationService.ReplaceAsync(new RegistrationPage());
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task Compromise()
        {
            if (!this.IsPersonal)
                return;

            try
            {
                if (!await this.uiDispatcher.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.Cancel))
                    return;

                (bool succeeded, LegalIdentity compromisedIdentity) = await this.networkService.Request(this.neuronService.Contracts.CompromisedLegalIdentityAsync, this.LegalIdentity.Id);

                if (succeeded)
                {
                    this.LegalIdentity = compromisedIdentity;
                    this.tagProfile.RevokeLegalIdentity(compromisedIdentity);
                    await this.navigationService.ReplaceAsync(new RegistrationPage());
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }
    }
}