using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Data.Countries;
using IdApp.Services.UI.Photos;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.Forms;

namespace IdApp.Pages.Identity.PetitionIdentity
{
    /// <summary>
    /// The view model to bind to when displaying petitioning of an identity in a view or page.
    /// </summary>
    public class PetitionIdentityViewModel : BaseViewModel
    {
        private string requestorFullJid;
        private string requestedIdentityId;
        private string petitionId;
        private string purpose;
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="PetitionIdentityViewModel"/> class.
        /// </summary>
        protected internal PetitionIdentityViewModel()
        {
            this.AcceptCommand = new Command(async _ => await this.Accept());
            this.DeclineCommand = new Command(async _ => await this.Decline());
            this.IgnoreCommand = new Command(async _ => await this.Ignore());
            this.AddContactCommand = new Command(async _ => await this.AddContact(), _ => !this.ThirdPartyInContacts);
            this.RemoveContactCommand = new Command(async _ => await this.RemoveContact(), _ => this.ThirdPartyInContacts);

            this.Photos = new ObservableCollection<Photo>();
            this.photosLoader = new PhotosLoader(this.Photos);
        }

        /// <inheritdoc/>
        protected override async Task OnInitialize()
        {
            await base.OnInitialize();

            if (this.NavigationService.TryPopArgs(out PetitionIdentityNavigationArgs args))
            {
                this.RequestorIdentity = args.RequestorIdentity;
                this.requestorFullJid = args.RequestorFullJid;
                this.requestedIdentityId = args.RequestedIdentityId;
                this.petitionId = args.PetitionId;
                this.purpose = args.Purpose;

                string BareJid = XmppClient.GetBareJID(this.requestorFullJid);

                ContactInfo Info = await ContactInfo.FindByBareJid(BareJid);

                if (!(Info is null) &&
                    (Info.LegalIdentity is null || (
                    Info.LegalId != this.RequestorIdentity.Id &&
                    Info.LegalIdentity is not null &&
                    Info.LegalIdentity.Created < this.RequestorIdentity.Created &&
                    this.RequestorIdentity.State == IdentityState.Approved)))
                {
                    Info.LegalId = this.LegalId;
                    Info.LegalIdentity = this.RequestorIdentity;
                    Info.FriendlyName = ContactInfo.GetFriendlyName(this.RequestorIdentity);

                    await Database.Update(Info);
                    await Database.Provider.Flush();
                }

                this.ThirdPartyInContacts = !(Info is null);
            }

            this.AssignProperties();
            this.EvaluateAllCommands();
            
            this.ReloadPhotos();
        }

        private async void ReloadPhotos()
        {
            try
            {
                this.photosLoader.CancelLoadPhotos();

                Attachment[] Attachments = this.RequestorIdentity?.Attachments;

                if (Attachments is not null)
                {
                    Photo First = await this.photosLoader.LoadPhotos(Attachments, SignWith.LatestApprovedId);

                    this.FirstPhotoSource = First?.Source;
                    this.FirstPhotoRotation = First?.Rotation ?? 0;
                }
            }
            catch (Exception ex)
            {
                this.LogService.LogException(ex);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnDispose()
        {
            this.photosLoader.CancelLoadPhotos();

            await base.OnDispose();
        }

        private void EvaluateAllCommands()
        {
            this.EvaluateCommands(this.AddContactCommand, this.RemoveContactCommand);
        }

        /// <summary>
        /// The command to bind to for accepting the petition
        /// </summary>
        public ICommand AcceptCommand { get; }

        /// <summary>
        /// The command to bind to for declining the petition
        /// </summary>
        public ICommand DeclineCommand { get; }

        /// <summary>
        /// The command to bind to for ignoring the petition
        /// </summary>
        public ICommand IgnoreCommand { get; }

        /// <summary>
        /// The command for adding the identity to the list of contacts.
        /// </summary>
        public ICommand AddContactCommand { get; }

        /// <summary>
        /// The command for removing the identity from the list of contacts.
        /// </summary>
        public ICommand RemoveContactCommand { get; }

        /// <summary>
        /// The list of photos related to the identity being petitioned.
        /// </summary>
        public ObservableCollection<Photo> Photos { get; }

        /// <summary>
        /// The identity of the requestor.
        /// </summary>
        public LegalIdentity RequestorIdentity { get; private set; }

        private async Task Accept()
        {
            if (!await App.VerifyPin())
                return;

            bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionIdentityResponse(this.requestedIdentityId, this.petitionId, this.requestorFullJid, true));
            if (succeeded)
                await this.NavigationService.GoBackAsync();
        }

        private async Task Decline()
        {
            bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionIdentityResponse(this.requestedIdentityId, this.petitionId, this.requestorFullJid, false));
            if (succeeded)
                await this.NavigationService.GoBackAsync();
        }

        private async Task Ignore()
        {
            await this.NavigationService.GoBackAsync();
        }

        #region Properties

        /// <summary>
        /// See <see cref="Created"/>
        /// </summary>
        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create(nameof(Created), typeof(DateTime), typeof(PetitionIdentityViewModel), default(DateTime));

        /// <summary>
        /// Created date of the identity
        /// </summary>
        public DateTime Created
        {
            get => (DateTime)this.GetValue(CreatedProperty);
            set => this.SetValue(CreatedProperty, value);
        }

        /// <summary>
        /// See <see cref="Updated"/>
        /// </summary>
        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create(nameof(Updated), typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// Updated date of the identity
        /// </summary>
        public DateTime? Updated
        {
            get { return (DateTime?)this.GetValue(UpdatedProperty); }
            set => this.SetValue(UpdatedProperty, value);
        }

        /// <summary>
        /// See <see cref="LegalId"/>
        /// </summary>
        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create(nameof(LegalId), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Legal id of the identity
        /// </summary>
        public string LegalId
        {
            get => (string)this.GetValue(LegalIdProperty);
            set => this.SetValue(LegalIdProperty, value);
        }

        /// <summary>
        /// See <see cref="State"/>
        /// </summary>
        public static readonly BindableProperty StateProperty =
            BindableProperty.Create(nameof(State), typeof(IdentityState), typeof(PetitionIdentityViewModel), default(IdentityState));

        /// <summary>
        /// Current state of the identity
        /// </summary>
        public IdentityState State
        {
            get => (IdentityState)this.GetValue(StateProperty);
            set => this.SetValue(StateProperty, value);
        }

        /// <summary>
        /// See <see cref="From"/>
        /// </summary>
        public static readonly BindableProperty FromProperty =
            BindableProperty.Create(nameof(From), typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// From date (validity range) of the identity
        /// </summary>
        public DateTime? From
        {
            get { return (DateTime?)this.GetValue(FromProperty); }
            set => this.SetValue(FromProperty, value);
        }

        /// <summary>
        /// See <see cref="To"/>
        /// </summary>
        public static readonly BindableProperty ToProperty =
            BindableProperty.Create(nameof(To), typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// To date (validity range) of the identity
        /// </summary>
        public DateTime? To
        {
            get { return (DateTime?)this.GetValue(ToProperty); }
            set => this.SetValue(ToProperty, value);
        }

        /// <summary>
        /// See <see cref="FirstName"/>
        /// </summary>
        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create(nameof(FirstName), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// First name of the identity
        /// </summary>
        public string FirstName
        {
            get => (string)this.GetValue(FirstNameProperty);
            set => this.SetValue(FirstNameProperty, value);
        }

        /// <summary>
        /// See <see cref="MiddleNames"/>
        /// </summary>
        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Middle name(s) of the identity
        /// </summary>
        public string MiddleNames
        {
            get => (string)this.GetValue(MiddleNamesProperty);
            set => this.SetValue(MiddleNamesProperty, value);
        }

        /// <summary>
        /// See <see cref="LastNames"/>
        /// </summary>
        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create(nameof(LastNames), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Last name(s) of the identity
        /// </summary>
        public string LastNames
        {
            get => (string)this.GetValue(LastNamesProperty);
            set => this.SetValue(LastNamesProperty, value);
        }

        /// <summary>
        /// See <see cref="PersonalNumber"/>
        /// </summary>
        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create(nameof(PersonalNumber), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Personal number of the identity
        /// </summary>
        public string PersonalNumber
        {
            get => (string)this.GetValue(PersonalNumberProperty);
            set => this.SetValue(PersonalNumberProperty, value);
        }

        /// <summary>
        /// See <see cref="Address"/>
        /// </summary>
        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create(nameof(Address), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Address of the identity
        /// </summary>
        public string Address
        {
            get => (string)this.GetValue(AddressProperty);
            set => this.SetValue(AddressProperty, value);
        }

        /// <summary>
        /// See <see cref="Address2"/>
        /// </summary>
        public static readonly BindableProperty Address2Property =
            BindableProperty.Create(nameof(Address2), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Address (line 2) of the identity
        /// </summary>
        public string Address2
        {
            get => (string)this.GetValue(Address2Property);
            set => this.SetValue(Address2Property, value);
        }

        /// <summary>
        /// See <see cref="ZipCode"/>
        /// </summary>
        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create(nameof(ZipCode), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Zip code of the identity
        /// </summary>
        public string ZipCode
        {
            get => (string)this.GetValue(ZipCodeProperty);
            set => this.SetValue(ZipCodeProperty, value);
        }

        /// <summary>
        /// See <see cref="Area"/>
        /// </summary>
        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create(nameof(Area), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Area of the identity
        /// </summary>
        public string Area
        {
            get => (string)this.GetValue(AreaProperty);
            set => this.SetValue(AreaProperty, value);
        }

        /// <summary>
        /// See <see cref="City"/>
        /// </summary>
        public static readonly BindableProperty CityProperty =
            BindableProperty.Create(nameof(City), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// City of the identity
        /// </summary>
        public string City
        {
            get => (string)this.GetValue(CityProperty);
            set => this.SetValue(CityProperty, value);
        }

        /// <summary>
        /// See <see cref="Region"/>
        /// </summary>
        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create(nameof(Region), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Region of the identity
        /// </summary>
        public string Region
        {
            get => (string)this.GetValue(RegionProperty);
            set => this.SetValue(RegionProperty, value);
        }

        /// <summary>
        /// See <see cref="CountryCode"/>
        /// </summary>
        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create(nameof(CountryCode), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Country code of the identity
        /// </summary>
        public string CountryCode
        {
            get => (string)this.GetValue(CountryCodeProperty);
            set => this.SetValue(CountryCodeProperty, value);
        }

        /// <summary>
        /// See <see cref="Country"/>
        /// </summary>
        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create(nameof(Country), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Country of the identity
        /// </summary>
        public string Country
        {
            get => (string)this.GetValue(CountryProperty);
            set => this.SetValue(CountryProperty, value);
        }

        /// <summary>
        /// See <see cref="PhoneNr"/>
        /// </summary>
        public static readonly BindableProperty PhoneNrProperty =
            BindableProperty.Create(nameof(PhoneNr), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// PhoneNr of the identity
        /// </summary>
        public string PhoneNr
        {
            get => (string)this.GetValue(PhoneNrProperty);
            set => this.SetValue(PhoneNrProperty, value);
        }

		/// <summary>
		/// See <see cref="EMail"/>
		/// </summary>
		public static readonly BindableProperty EMailProperty =
			BindableProperty.Create(nameof(EMail), typeof(string), typeof(PetitionIdentityViewModel), default(string));

		/// <summary>
		/// EMail of the identity
		/// </summary>
		public string EMail
		{
			get => (string)this.GetValue(EMailProperty);
			set => this.SetValue(EMailProperty, value);
		}

		/// <summary>
		/// See <see cref="IsApproved"/>
		/// </summary>
		public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create(nameof(IsApproved), typeof(bool), typeof(PetitionIdentityViewModel), default(bool));

        /// <summary>
        /// Is the contract approved?
        /// </summary>
        public bool IsApproved
        {
            get => (bool)this.GetValue(IsApprovedProperty);
            set => this.SetValue(IsApprovedProperty, value);
        }

        /// <summary>
        /// See <see cref="Purpose"/>
        /// </summary>
        public static readonly BindableProperty PurposeProperty =
            BindableProperty.Create(nameof(Purpose), typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// What's the purpose of the petition?
        /// </summary>
        public string Purpose
        {
            get => (string)this.GetValue(PurposeProperty);
            set => this.SetValue(PurposeProperty, value);
        }

        /// <summary>
        /// See <see cref="ThirdPartyInContacts"/>
        /// </summary>
        public static readonly BindableProperty ThirdPartyInContactsProperty =
            BindableProperty.Create(nameof(ThirdPartyInContacts), typeof(bool), typeof(PetitionIdentityViewModel), default(bool));

        /// <summary>
		/// Gets or sets whether the identity is in the contact list or not.
        /// </summary>
        public bool ThirdPartyInContacts
        {
            get => (bool)this.GetValue(ThirdPartyInContactsProperty);
            set => this.SetValue(ThirdPartyInContactsProperty, value);
        }

        /// <summary>
        /// See <see cref="FirstPhotoSource"/>
        /// </summary>
        public static readonly BindableProperty FirstPhotoSourceProperty =
            BindableProperty.Create(nameof(FirstPhotoSource), typeof(ImageSource), typeof(PetitionIdentityViewModel), default(ImageSource));

        /// <summary>
        /// Image source of the first photo in the identity of the requestor.
        /// </summary>
        public ImageSource FirstPhotoSource
        {
            get => (ImageSource)this.GetValue(FirstPhotoSourceProperty);
            set => this.SetValue(FirstPhotoSourceProperty, value);
        }

        /// <summary>
        /// See <see cref="FirstPhotoRotation"/>
        /// </summary>
        public static readonly BindableProperty FirstPhotoRotationProperty =
            BindableProperty.Create(nameof(FirstPhotoRotation), typeof(int), typeof(PetitionIdentityViewModel), default(int));

        /// <summary>
        /// Rotation of the first photo in the identity of the requestor.
        /// </summary>
        public int FirstPhotoRotation
        {
            get => (int)this.GetValue(FirstPhotoRotationProperty);
            set => this.SetValue(FirstPhotoRotationProperty, value);
        }

        #endregion

        private void AssignProperties()
        {
            if (!(this.RequestorIdentity is null))
            {
                this.Created = this.RequestorIdentity.Created;
                this.Updated = this.RequestorIdentity.Updated.GetDateOrNullIfMinValue();
                this.LegalId = this.RequestorIdentity.Id;
                this.State = this.RequestorIdentity.State;
                this.From = this.RequestorIdentity.From.GetDateOrNullIfMinValue();
                this.To = this.RequestorIdentity.To.GetDateOrNullIfMinValue();
                this.FirstName = this.RequestorIdentity[Constants.XmppProperties.FirstName];
                this.MiddleNames = this.RequestorIdentity[Constants.XmppProperties.MiddleName];
                this.LastNames = this.RequestorIdentity[Constants.XmppProperties.LastName];
                this.PersonalNumber = this.RequestorIdentity[Constants.XmppProperties.PersonalNumber];
                this.Address = this.RequestorIdentity[Constants.XmppProperties.Address];
                this.Address2 = this.RequestorIdentity[Constants.XmppProperties.Address2];
                this.ZipCode = this.RequestorIdentity[Constants.XmppProperties.ZipCode];
                this.Area = this.RequestorIdentity[Constants.XmppProperties.Area];
                this.City = this.RequestorIdentity[Constants.XmppProperties.City];
                this.Region = this.RequestorIdentity[Constants.XmppProperties.Region];
                this.CountryCode = this.RequestorIdentity[Constants.XmppProperties.Country];
                this.Country = ISO_3166_1.ToName(this.CountryCode);
                this.PhoneNr = this.RequestorIdentity[Constants.XmppProperties.Phone];
                this.EMail = this.RequestorIdentity[Constants.XmppProperties.EMail];
                this.IsApproved = this.RequestorIdentity.State == IdentityState.Approved;
            }
            else
            {
                this.Created = DateTime.MinValue;
                this.Updated = null;
                this.LegalId = Constants.NotAvailableValue;
                this.State = IdentityState.Compromised;
                this.From = null;
                this.To = null;
                this.FirstName = Constants.NotAvailableValue;
                this.MiddleNames = Constants.NotAvailableValue;
                this.LastNames = Constants.NotAvailableValue;
                this.PersonalNumber = Constants.NotAvailableValue;
                this.Address = Constants.NotAvailableValue;
                this.Address2 = Constants.NotAvailableValue;
                this.ZipCode = Constants.NotAvailableValue;
                this.Area = Constants.NotAvailableValue;
                this.City = Constants.NotAvailableValue;
                this.Region = Constants.NotAvailableValue;
                this.CountryCode = Constants.NotAvailableValue;
                this.Country = Constants.NotAvailableValue;
                this.PhoneNr = Constants.NotAvailableValue;
                this.EMail = Constants.NotAvailableValue;
                this.IsApproved = false;
            }
            this.Purpose = this.purpose;
        }

        private async Task AddContact()
        {
            try
            {
                string FriendlyName = ContactInfo.GetFriendlyName(this.RequestorIdentity);
                string BareJid = XmppClient.GetBareJID(this.requestorFullJid);

                RosterItem Item = this.XmppService.Xmpp[BareJid];
                if (Item is null)
                    this.XmppService.Xmpp.AddRosterItem(new RosterItem(BareJid, FriendlyName));

                ContactInfo Info = await ContactInfo.FindByBareJid(BareJid);
                if (Info is null)
                {
                    Info = new ContactInfo()
                    {
                        BareJid = BareJid,
                        LegalId = this.RequestorIdentity.Id,
                        LegalIdentity = this.RequestorIdentity,
                        FriendlyName = FriendlyName,
                        IsThing = false
                    };

                    await Database.Insert(Info);
                }
                else
                {
                    Info.LegalId = this.requestedIdentityId;
                    Info.LegalIdentity = this.RequestorIdentity;
                    Info.FriendlyName = FriendlyName;

                    await Database.Update(Info);
                }

                await this.AttachmentCacheService.MakePermanent(this.LegalId);
                await Database.Provider.Flush();

                this.ThirdPartyInContacts = true;

                this.EvaluateAllCommands();
            }
            catch (Exception ex)
            {
                await this.UiSerializer.DisplayAlert(ex);
            }
        }

        private async Task RemoveContact()
        {
            try
            {
                if (!await this.UiSerializer.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToRemoveContact, AppResources.Yes, AppResources.Cancel))
                    return;

                string BareJid = XmppClient.GetBareJID(this.requestorFullJid);

                ContactInfo Info = await ContactInfo.FindByBareJid(BareJid);
                if (!(Info is null))
                {
                    await Database.Delete(Info);
                    await this.AttachmentCacheService.MakeTemporary(Info.LegalId);
                    await Database.Provider.Flush();
                }

                RosterItem Item = this.XmppService.Xmpp[BareJid];
                if (!(Item is null))
                    this.XmppService.Xmpp.RemoveRosterItem(BareJid);

                this.ThirdPartyInContacts = false;

                this.EvaluateAllCommands();
            }
            catch (Exception ex)
            {
                await this.UiSerializer.DisplayAlert(ex);
            }
        }

    }
}
