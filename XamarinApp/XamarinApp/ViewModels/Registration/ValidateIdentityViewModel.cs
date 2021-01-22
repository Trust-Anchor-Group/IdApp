using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Extensions;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.Extensions;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Views;

namespace XamarinApp.ViewModels.Registration
{
    public class ValidateIdentityViewModel : RegistrationStepViewModel
    {
        private readonly INetworkService networkService;
        private readonly PhotosLoader photosLoader;

        public ValidateIdentityViewModel(
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService,
            INetworkService networkService,
            ILogService logService)
            : base(RegistrationStep.ValidateIdentity, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
        {
            this.networkService = networkService;
            this.InviteReviewerCommand = new Command(async _ => await InviteReviewer(), _ => this.State == IdentityState.Created && this.NeuronService.IsOnline);
            this.ContinueCommand = new Command(_ => Continue(), _ => IsApproved);
            this.Title = AppResources.ValidatingInformation;
            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(logService, networkService, neuronService, this.Photos);
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            AssignProperties();

            this.TagProfile.Changed += TagProfile_Changed;
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
            this.NeuronService.Contracts.LegalIdentityChanged += NeuronContracts_LegalIdentityChanged;
        }

        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            this.TagProfile.Changed -= TagProfile_Changed;
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            this.NeuronService.Contracts.LegalIdentityChanged -= NeuronContracts_LegalIdentityChanged;
            await base.DoUnbind();
        }

        public ObservableCollection<ImageSource> Photos { get; }

        public ICommand InviteReviewerCommand { get; }
        public ICommand ContinueCommand { get; }

        private void AssignProperties()
        {
            Created = this.TagProfile.LegalIdentity?.Created ?? DateTime.MinValue;
            Updated = this.TagProfile.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
            LegalId = this.TagProfile.LegalIdentity?.Id;
            LegalIdentity = this.TagProfile.LegalIdentity;
            AssignBareJId();
            State = this.TagProfile.LegalIdentity?.State ?? IdentityState.Rejected;
            From = this.TagProfile.LegalIdentity?.From.GetDateOrNullIfMinValue();
            To = this.TagProfile.LegalIdentity?.To.GetDateOrNullIfMinValue();
            if (this.TagProfile.LegalIdentity != null)
            {
                FirstName = this.TagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
                MiddleNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.MiddleName];
                LastNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.LastName];
                PersonalNumber = this.TagProfile.LegalIdentity[Constants.XmppProperties.PersonalNumber];
                Address = this.TagProfile.LegalIdentity[Constants.XmppProperties.Address];
                Address2 = this.TagProfile.LegalIdentity[Constants.XmppProperties.Address2];
                ZipCode = this.TagProfile.LegalIdentity[Constants.XmppProperties.ZipCode];
                Area = this.TagProfile.LegalIdentity[Constants.XmppProperties.Area];
                City = this.TagProfile.LegalIdentity[Constants.XmppProperties.City];
                Region = this.TagProfile.LegalIdentity[Constants.XmppProperties.Region];
                CountryCode = this.TagProfile.LegalIdentity[Constants.XmppProperties.Country];
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
            IsApproved = this.TagProfile.LegalIdentity?.State == IdentityState.Approved;
            IsCreated = this.TagProfile.LegalIdentity?.State == IdentityState.Created;

            ContinueCommand.ChangeCanExecute();
            InviteReviewerCommand.ChangeCanExecute();

            SetConnectionStateAndText(this.NeuronService.State);

            if (this.IsConnected)
            {
                this.ReloadPhotos();
            }
        }

        private void AssignBareJId()
        {
            BareJId = this.NeuronService?.BareJId ?? string.Empty;
        }

        private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.TagProfile.Step) || e.PropertyName == nameof(this.TagProfile.LegalIdentity))
            {
                UiDispatcher.BeginInvokeOnMainThread(AssignProperties);
            }
            else
            {
                UiDispatcher.BeginInvokeOnMainThread(AssignBareJId);
            }
        }

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(async () =>
            {
                this.AssignBareJId();
                this.SetConnectionStateAndText(e.State);
                this.InviteReviewerCommand.ChangeCanExecute();
                if (this.IsConnected)
                {
                    await Task.Delay(Constants.Timeouts.XmppInit);
                    this.ReloadPhotos();
                }
            });
        }

        private void SetConnectionStateAndText(XmppState state)
        {
            IsConnected = state == XmppState.Connected;
            this.ConnectionStateText = state.ToDisplayText(null);
        }

        private void ReloadPhotos()
        {
            this.photosLoader.CancelLoadPhotos();
            if (this.TagProfile?.LegalIdentity?.Attachments != null)
            {
                _ = this.photosLoader.LoadPhotos(this.TagProfile.LegalIdentity.Attachments);
            }
        }

        private void NeuronContracts_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
        {
            UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.LegalIdentity = e.Identity;
                this.TagProfile.SetLegalIdentity(e.Identity);
                AssignProperties();
            });
        }

        #region Properties

        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create("Created", typeof(DateTime), typeof(ValidateIdentityViewModel), default(DateTime));

        public DateTime Created
        {
            get { return (DateTime)GetValue(CreatedProperty); }
            set { SetValue(CreatedProperty, value); }
        }

        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create("Updated", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        public DateTime? Updated
        {
            get { return (DateTime?)GetValue(UpdatedProperty); }
            set { SetValue(UpdatedProperty, value); }
        }

        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string LegalId
        {
            get { return (string)GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        public LegalIdentity LegalIdentity { get; private set; }

        public static readonly BindableProperty BareJIdProperty =
            BindableProperty.Create("BareJId", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string BareJId
        {
            get { return (string)GetValue(BareJIdProperty); }
            set { SetValue(BareJIdProperty, value); }
        }

        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(IdentityState), typeof(ValidateIdentityViewModel), default(IdentityState));

        public IdentityState State
        {
            get { return (IdentityState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        public DateTime? From
        {
            get { return (DateTime?)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        public DateTime? To
        {
            get { return (DateTime?)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string MiddleNames
        {
            get { return (string)GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string LastNames
        {
            get { return (string)GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Address2
        {
            get { return (string)GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string ZipCode
        {
            get { return (string)GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Area
        {
            get { return (string)GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string CountryCode
        {
            get { return (string)GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        public bool IsApproved
        {
            get { return (bool)GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        public static readonly BindableProperty IsCreatedProperty =
            BindableProperty.Create("IsCreated", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        public bool IsCreated
        {
            get { return (bool)GetValue(IsCreatedProperty); }
            set { SetValue(IsCreatedProperty, value); }
        }

        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        public bool IsConnected
        {
            get { return (bool) GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string) GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        #endregion

        private async Task InviteReviewer()
        {
            string code = await ScanQrCodePage.ScanQrCode(this.NavigationService, AppResources.Open);

            if (!Constants.IoTSchemes.StartsWithIdScheme(code))
            {
                await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity);
                return;
            }

            bool succeeded = await this.networkService.TryRequest(this.NeuronService.Contracts.PetitionPeerReviewIdAsync, Constants.IoTSchemes.GetCode(code), this.TagProfile.LegalIdentity, Guid.NewGuid().ToString(), AppResources.CouldYouPleaseReviewMyIdentityInformation);
            if (succeeded)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToYourPeer);
            }
        }

        private void Continue()
        {
            this.TagProfile.SetIsValidated();
            this.OnStepCompleted(EventArgs.Empty);
        }
    }
}