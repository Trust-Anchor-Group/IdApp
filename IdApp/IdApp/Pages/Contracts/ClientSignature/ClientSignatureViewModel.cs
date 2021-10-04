using System;
using System.Globalization;
using System.Threading.Tasks;
using IdApp.Pages.Identity.PetitionIdentity;
using IdApp.Extensions;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using IdApp.Services.Navigation;

namespace IdApp.Pages.Contracts.ClientSignature
{
    /// <summary>
    /// The view model to bind to for when displaying client signatures.
    /// </summary>
    public class ClientSignatureViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        private Waher.Networking.XMPP.Contracts.ClientSignature signature;
        private LegalIdentity identity;

        /// <summary>
        /// Creates an instance of the <see cref="ClientSignatureViewModel"/> class.
        /// </summary>
        public ClientSignatureViewModel()
            : this(null)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ClientSignatureViewModel"/> class.
        /// For unit tests.
        /// <param name="navigationService">The navigation service.</param>
        /// </summary>
        protected internal ClientSignatureViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService ?? App.Instantiate<INavigationService>();
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();

            if (this.navigationService.TryPopArgs(out ClientSignatureNavigationArgs args))
            {
                this.signature = args.Signature;
                this.identity = args.Identity;
            }
            
            AssignProperties();
        }

        #region Properties

        /// <summary>
        /// See <see cref="Created"/>
        /// </summary>
        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create("Created", typeof(DateTime), typeof(PetitionIdentityViewModel), default(DateTime));

        /// <summary>
        /// The Created date of the signature
        /// </summary>
        public DateTime Created
        {
            get { return (DateTime) GetValue(CreatedProperty); }
            set { SetValue(CreatedProperty, value); }
        }

        /// <summary>
        /// See <see cref="Updated"/>
        /// </summary>
        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create("Updated", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        ///  The Updated timestamp of the signature
        /// </summary>
        public DateTime? Updated
        {
            get { return (DateTime?) GetValue(UpdatedProperty); }
            set { SetValue(UpdatedProperty, value); }
        }

        /// <summary>
        /// <see cref="LegalId"/>
        /// </summary>
        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The Legal id of the signature
        /// </summary>
        public string LegalId
        {
            get { return (string) GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        /// <summary>
        /// See <see cref="State"/>
        /// </summary>
        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The current state of the signature
        /// </summary>
        public string State
        {
            get { return (string) GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        /// <summary>
        /// See <see cref="From"/>
        /// </summary>
        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// The from date of the signature
        /// </summary>
        public DateTime? From
        {
            get { return (DateTime?) GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        /// <summary>
        /// See <see cref="To"/>
        /// </summary>
        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// The to date of the signature
        /// </summary>
        public DateTime? To
        {
            get { return (DateTime?) GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        /// <summary>
        /// See <see cref="FirstName"/>
        /// </summary>
        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's first name property
        /// </summary>
        public string FirstName
        {
            get { return (string) GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        /// <summary>
        /// See <see cref="MiddleNames"/>
        /// </summary>
        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's middle names property
        /// </summary>
        public string MiddleNames
        {
            get { return (string) GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        /// <summary>
        /// See <see cref="LastNames"/>
        /// </summary>
        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's last names property
        /// </summary>
        public string LastNames
        {
            get { return (string) GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        /// <summary>
        /// See <see cref="PersonalNumber"/>
        /// </summary>
        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's personal number property
        /// </summary>
        public string PersonalNumber
        {
            get { return (string) GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        /// <summary>
        /// See <see cref="Address"/>
        /// </summary>
        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's address property
        /// </summary>
        public string Address
        {
            get { return (string) GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        /// <summary>
        /// See <see cref="Address2"/>
        /// </summary>
        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's address line 2 property
        /// </summary>
        public string Address2
        {
            get { return (string) GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        /// <summary>
        /// See <see cref="ZipCode"/>
        /// </summary>
        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's zip code property
        /// </summary>
        public string ZipCode
        {
            get { return (string) GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        /// <summary>
        /// See <see cref="Area"/>
        /// </summary>
        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's Area property
        /// </summary>
        public string Area
        {
            get { return (string) GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        /// <summary>
        /// See <see cref="City"/>
        /// </summary>
        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's city property
        /// </summary>
        public string City
        {
            get { return (string) GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        /// <summary>
        /// See <see cref="Region"/>
        /// </summary>
        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's region property
        /// </summary>
        public string Region
        {
            get { return (string) GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        /// <summary>
        /// See <see cref="CountryCode"/>
        /// </summary>
        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's country code property
        /// </summary>
        public string CountryCode
        {
            get { return (string) GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        /// <summary>
        /// See <see cref="Country"/>
        /// </summary>
        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// The legal identity's country property
        /// </summary>
        public string Country
        {
            get { return (string) GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        /// <summary>
        /// See <see cref="IsApproved"/>
        /// </summary>
        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(PetitionIdentityViewModel), default(bool));

        /// <summary>
        /// Determines whether the identity is approved or not.
        /// </summary>
        public bool IsApproved
        {
            get { return (bool) GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        /// <summary>
        /// See <see cref="Role"/>
        /// </summary>
        public static readonly BindableProperty RoleProperty =
            BindableProperty.Create("Role", typeof(string), typeof(ClientSignatureViewModel), default(string));

        /// <summary>
        /// The role of the signature
        /// </summary>
        public string Role
        {
            get { return (string) GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        /// <summary>
        /// See <see cref="Timestamp"/>
        /// </summary>
        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create("Timestamp", typeof(string), typeof(ClientSignatureViewModel), default(string));

        /// <summary>
        /// The signature's timestamp.
        /// </summary>
        public string Timestamp
        {
            get { return (string) GetValue(TimestampProperty); }
            set { SetValue(TimestampProperty, value); }
        }

        /// <summary>
        /// <see cref="IsTransferable"/>
        /// </summary>
        public static readonly BindableProperty IsTransferableProperty =
            BindableProperty.Create("IsTransferable", typeof(string), typeof(ClientSignatureViewModel), default(string));

        /// <summary>
        /// Determines whether the signature is transferable or not.
        /// </summary>
        public string IsTransferable
        {
            get { return (string) GetValue(IsTransferableProperty); }
            set { SetValue(IsTransferableProperty, value); }
        }

        /// <summary>
        /// See <see cref="BareJid"/>
        /// </summary>
        public static readonly BindableProperty BareJidProperty =
            BindableProperty.Create("BareJid", typeof(string), typeof(ClientSignatureViewModel), default(string));

        /// <summary>
        /// Gets or sets the Bare Jid of the signature.
        /// </summary>
        public string BareJid
        {
            get { return (string) GetValue(BareJidProperty); }
            set { SetValue(BareJidProperty, value); }
        }

        /// <summary>
        /// See <see cref="PhoneNr"/>
        /// </summary>
        public static readonly BindableProperty PhoneNrProperty =
            BindableProperty.Create("PhoneNr", typeof(string), typeof(ClientSignatureViewModel), default(string));

        /// <summary>
        /// Gets or sets the Bare Jid of the signature.
        /// </summary>
        public string PhoneNr
        {
            get { return (string)GetValue(PhoneNrProperty); }
            set { SetValue(PhoneNrProperty, value); }
        }

        /// <summary>
        /// See <see cref="Signature"/>
        /// </summary>
        public static readonly BindableProperty SignatureProperty =
            BindableProperty.Create("Signature", typeof(string), typeof(ClientSignatureViewModel), default(string));

        /// <summary>
        /// The signature in plain text.
        /// </summary>
        public string Signature
        {
            get { return (string) GetValue(SignatureProperty); }
            set { SetValue(SignatureProperty, value); }
        }

        #endregion

        private void AssignProperties()
        {
            if (!(identity is null))
            {
                this.Created = identity.Created;
                this.Updated = identity.Updated.GetDateOrNullIfMinValue();
                this.LegalId = identity.Id;
                this.State = identity.State.ToString();
                this.From = identity.From.GetDateOrNullIfMinValue();
                this.To = identity.To.GetDateOrNullIfMinValue();
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
                this.CountryCode = identity[Constants.XmppProperties.Country];
                this.Country = ISO_3166_1.ToName(this.CountryCode);
                this.IsApproved = identity.State == IdentityState.Approved;
                this.BareJid = identity.GetJid(Constants.NotAvailableValue);
                this.PhoneNr = identity[Constants.XmppProperties.Phone];
            }
            else
            {
                this.Created = DateTime.MinValue;
                this.Updated = DateTime.MinValue;
                this.LegalId = Constants.NotAvailableValue;
                this.State = Constants.NotAvailableValue;
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
                this.IsApproved = false;
                this.BareJid = Constants.NotAvailableValue;
                this.PhoneNr = Constants.NotAvailableValue;
            }
            if (!(signature is null))
            {
                this.Role = signature.Role;
                this.Timestamp = signature.Timestamp.ToString(CultureInfo.CurrentUICulture);
                this.IsTransferable = signature.Transferable ? AppResources.Yes : AppResources.No;
                this.BareJid = signature.BareJid;
                this.Signature = Convert.ToBase64String(signature.DigitalSignature);
            }
            else
            {
                this.Role = Constants.NotAvailableValue;
                this.Timestamp = Constants.NotAvailableValue;
                this.IsTransferable = AppResources.No;
                this.Signature = Constants.NotAvailableValue;
            }
        }
    }
}