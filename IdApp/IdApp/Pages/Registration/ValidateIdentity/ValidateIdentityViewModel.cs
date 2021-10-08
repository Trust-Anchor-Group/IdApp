using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Services.AttachmentCache;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using IdApp.Services.UI.Photos;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Pages.Registration.ValidateIdentity
{
    /// <summary>
    /// The view model to bind to when showing Step 4 of the registration flow: validating an identity.
    /// </summary>
    public class ValidateIdentityViewModel : RegistrationStepViewModel
    {
        private readonly INetworkService networkService;
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="ValidateIdentityViewModel"/> class.
        /// <param name="tagProfile">The tag profile to work with.</param>
        /// <param name="uiSerializer">The UI dispatcher for alerts.</param>
        /// <param name="neuronService">The Neuron service for XMPP communication.</param>
        /// <param name="navigationService">The navigation service to use for app navigation</param>
        /// <param name="settingsService">The settings service for persisting UI state.</param>
        /// <param name="networkService">The network service for network access.</param>
        /// <param name="logService">The log service.</param>
        /// <param name="attachmentCacheService">The attachment cache to use.</param>
        /// </summary>
        public ValidateIdentityViewModel(
            ITagProfile tagProfile,
            IUiSerializer uiSerializer,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService,
            INetworkService networkService,
            ILogService logService,
            IAttachmentCacheService attachmentCacheService)
            : base(RegistrationStep.ValidateIdentity, tagProfile, uiSerializer, neuronService, navigationService, settingsService, logService)
        {
            this.networkService = networkService;
            this.InviteReviewerCommand = new Command(async _ => await InviteReviewer(), _ => this.State == IdentityState.Created && this.NeuronService.IsOnline);
            this.ContinueCommand = new Command(_ => Continue(), _ => IsApproved);
            this.Title = AppResources.ValidatingInformation;
            this.Photos = new ObservableCollection<Photo>();
            this.photosLoader = new PhotosLoader(logService, networkService, neuronService, uiSerializer,
                attachmentCacheService ?? App.Instantiate<IAttachmentCacheService>(), this.Photos);
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            AssignProperties();

            this.TagProfile.Changed += TagProfile_Changed;
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
            this.NeuronService.Contracts.LegalIdentityChanged += NeuronContracts_LegalIdentityChanged;
        }

        /// <inheritdoc />
        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            this.TagProfile.Changed -= TagProfile_Changed;
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            this.NeuronService.Contracts.LegalIdentityChanged -= NeuronContracts_LegalIdentityChanged;
            await base.DoUnbind();
        }

        #region Properties

        /// <summary>
        /// The list of photos associated with this legal identity.
        /// </summary>
        public ObservableCollection<Photo> Photos { get; }

        /// <summary>
        /// The command to bind to for inviting a reviewer to approve the user's identity.
        /// </summary>
        public ICommand InviteReviewerCommand { get; }

        /// <summary>
        /// The command to bind to for continuing to the next step in the registration process.
        /// </summary>
        public ICommand ContinueCommand { get; }

        /// <summary>
        /// The <see cref="Created"/>
        /// </summary>
        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create("Created", typeof(DateTime), typeof(ValidateIdentityViewModel), default(DateTime));

        /// <summary>
        /// Gets or sets the Created time stamp of the legal identity.
        /// </summary>
        public DateTime Created
        {
            get { return (DateTime)GetValue(CreatedProperty); }
            set { SetValue(CreatedProperty, value); }
        }

        /// <summary>
        /// The <see cref="Updated"/>
        /// </summary>
        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create("Updated", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        /// <summary>
        /// Gets or sets the Updated time stamp of the legal identity.
        /// </summary>
        public DateTime? Updated
        {
            get { return (DateTime?)GetValue(UpdatedProperty); }
            set { SetValue(UpdatedProperty, value); }
        }

        /// <summary>
        /// The <see cref="LegalId"/>
        /// </summary>
        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// Gets or sets the Id of the legal identity.
        /// </summary>
        public string LegalId
        {
            get { return (string)GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the legal identity.
        /// </summary>
        public LegalIdentity LegalIdentity { get; private set; }

        /// <summary>
        /// The <see cref="BareJid"/>
        /// </summary>
        public static readonly BindableProperty BareJidProperty =
            BindableProperty.Create("BareJid", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// Gets or sets the Bare Jid registered with the Neuron server.
        /// </summary>
        public string BareJid
        {
            get { return (string)GetValue(BareJidProperty); }
            set { SetValue(BareJidProperty, value); }
        }

        /// <summary>
        /// The <see cref="State"/>
        /// </summary>
        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(IdentityState), typeof(ValidateIdentityViewModel), default(IdentityState));

        /// <summary>
        /// The current state of the user's legal identity.
        /// </summary>
        public IdentityState State
        {
            get { return (IdentityState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        /// <summary>
        /// The <see cref="From"/>
        /// </summary>
        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        /// <summary>
        /// Gets or sets the From time stamp (validity range) of the user's identity.
        /// </summary>
        public DateTime? From
        {
            get { return (DateTime?)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        /// <summary>
        /// The <see cref="To"/>
        /// </summary>
        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        /// <summary>
        /// Gets or sets the To time stamp (validity range) of the user's identity.
        /// </summary>
        public DateTime? To
        {
            get { return (DateTime?)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        /// <summary>
        /// The <see cref="FirstName"/>
        /// </summary>
        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("MiddleNames", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("LastNames", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// The user's personal number
        /// </summary>
        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        /// <summary>
        /// The <see cref="Address"/>
        /// </summary>
        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("Address2", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("ZipCode", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("Area", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("City", typeof(string), typeof(ValidateIdentityViewModel), default(string));

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
            BindableProperty.Create("Region", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// The user's region
        /// </summary>
        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        /// <summary>
        /// The <see cref="Country"/>
        /// </summary>
        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// Gets or sets the user's country.
        /// </summary>
        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        /// <summary>
        /// The <see cref="CountryCode"/>
        /// </summary>
        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// Gets or sets the user's country.
        /// </summary>
        public string CountryCode
        {
            get { return (string)GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        /// <summary>
        /// The <see cref="PhoneNr"/>
        /// </summary>
        public static readonly BindableProperty PhoneNrProperty =
            BindableProperty.Create("PhoneNr", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// Gets or sets the user's country.
        /// </summary>
        public string PhoneNr
        {
            get { return (string)GetValue(PhoneNrProperty); }
            set { SetValue(PhoneNrProperty, value); }
        }

        /// <summary>
        /// The <see cref="IsApproved"/>
        /// </summary>
        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        /// <summary>
        /// Gets or sets if the user's identity is approved or not.
        /// </summary>
        public bool IsApproved
        {
            get { return (bool)GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        /// <summary>
        /// The <see cref="IsCreated"/>
        /// </summary>
        public static readonly BindableProperty IsCreatedProperty =
            BindableProperty.Create("IsCreated", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        /// <summary>
        /// Gets or sets if the user's identity has been created.
        /// </summary>
        public bool IsCreated
        {
            get { return (bool)GetValue(IsCreatedProperty); }
            set { SetValue(IsCreatedProperty, value); }
        }

        /// <summary>
        /// The <see cref="IsConnected"/>
        /// </summary>
        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        /// <summary>
        /// Gets or sets if the app is connected to a Neuron server.
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
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        /// <summary>
        /// The user friendly connection state text to display to the user.
        /// </summary>
        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        #endregion

        private void AssignProperties()
        {
            Created = this.TagProfile.LegalIdentity?.Created ?? DateTime.MinValue;
            Updated = this.TagProfile.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
            LegalId = this.TagProfile.LegalIdentity?.Id;
            LegalIdentity = this.TagProfile.LegalIdentity;
            AssignBareJid();
            State = this.TagProfile.LegalIdentity?.State ?? IdentityState.Rejected;
            From = this.TagProfile.LegalIdentity?.From.GetDateOrNullIfMinValue();
            To = this.TagProfile.LegalIdentity?.To.GetDateOrNullIfMinValue();

            if (!(this.TagProfile.LegalIdentity is null))
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
                PhoneNr = this.TagProfile.LegalIdentity[Constants.XmppProperties.Phone];
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
                PhoneNr = string.Empty;
            }

            Country = ISO_3166_1.ToName(this.CountryCode);
            IsApproved = this.TagProfile.LegalIdentity?.State == IdentityState.Approved;
            IsCreated = this.TagProfile.LegalIdentity?.State == IdentityState.Created;

            ContinueCommand.ChangeCanExecute();
            InviteReviewerCommand.ChangeCanExecute();

            SetConnectionStateAndText(this.NeuronService.State);

            if (this.IsConnected)
                this.ReloadPhotos();
        }

        private void AssignBareJid()
        {
            BareJid = this.NeuronService?.BareJid ?? string.Empty;
        }

        private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.TagProfile.Step) || e.PropertyName == nameof(this.TagProfile.LegalIdentity))
                this.UiSerializer.BeginInvokeOnMainThread(AssignProperties);
            else
                this.UiSerializer.BeginInvokeOnMainThread(AssignBareJid);
        }

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiSerializer.BeginInvokeOnMainThread(async () =>
            {
                this.AssignBareJid();
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
            this.ConnectionStateText = state.ToDisplayText();
        }

        private void ReloadPhotos()
        {
            this.photosLoader.CancelLoadPhotos();
            if (!(this.TagProfile?.LegalIdentity?.Attachments is null))
            {
                _ = this.photosLoader.LoadPhotos(this.TagProfile.LegalIdentity.Attachments, SignWith.LatestApprovedIdOrCurrentKeys);
            }
        }

        private void NeuronContracts_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
        {
            this.UiSerializer.BeginInvokeOnMainThread(() =>
            {
                this.LegalIdentity = e.Identity;
                this.TagProfile.SetLegalIdentity(e.Identity);
                AssignProperties();
            });
        }

        private async Task InviteReviewer()
        {
            string code = await QrCode.ScanQrCode(this.NavigationService, AppResources.InvitePeerToReview);

            if (!Constants.UriSchemes.StartsWithIdScheme(code))
            {
                await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity);
                return;
            }

            bool succeeded = await this.networkService.TryRequest(() => this.NeuronService.Contracts.PetitionPeerReviewId(
                Constants.UriSchemes.GetCode(code), this.TagProfile.LegalIdentity, Guid.NewGuid().ToString(), AppResources.CouldYouPleaseReviewMyIdentityInformation));

            if (succeeded)
                await this.UiSerializer.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToYourPeer);
        }

        private void Continue()
        {
            this.TagProfile.SetIsValidated();
            this.OnStepCompleted(EventArgs.Empty);
        }
    }
}