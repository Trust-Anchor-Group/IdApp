using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using IdApp.Services.UI.Photos;
using IdApp.Services.Data.Countries;

namespace IdApp.Pages.Contracts.PetitionContract
{
    /// <summary>
    /// The view model to bind to when displaying petitioning of a contract in a view or page.
    /// </summary>
    public class PetitionContractViewModel : BaseViewModel
    {
        private LegalIdentity requestorIdentity;
        private string requestorFullJid;
        private string petitionId;
        private string purpose;
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="PetitionContractViewModel"/> class.
        /// </summary>
        protected internal PetitionContractViewModel()
        {
            this.AcceptCommand = new Command(async _ => await Accept());
            this.DeclineCommand = new Command(async _ => await Decline());
            this.IgnoreCommand = new Command(async _ => await Ignore());
            
            this.Photos = new ObservableCollection<Photo>();
            this.photosLoader = new PhotosLoader(this.Photos);
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();

            if (this.NavigationService.TryPopArgs(out PetitionContractNavigationArgs args))
            {
                this.requestorIdentity = args.RequestorIdentity;
                this.requestorFullJid = args.RequestorFullJid;
                this.RequestedContract = args.RequestedContract;
                this.petitionId = args.PetitionId;
                this.purpose = args.Purpose;
            }
            this.AssignProperties();
            if (!(this.requestorIdentity?.Attachments is null))
            {
                _ = this.photosLoader.LoadPhotos(this.requestorIdentity.Attachments, SignWith.LatestApprovedId);
            }
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            await base.DoUnbind();
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
        /// The list of photos related to the contract being petitioned.
        /// </summary>
        public ObservableCollection<Photo> Photos { get; }

        /// <summary>
        /// The contract to display.
        /// </summary>
        public Contract RequestedContract { get; private set; }

        private async Task Accept()
        {
            if (!await App.VerifyPin())
                return;

            bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionContractResponse(this.RequestedContract.ContractId, this.petitionId, this.requestorFullJid, true));
            if (succeeded)
                await this.NavigationService.GoBackAsync();
        }

        private async Task Decline()
        {
            bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionContractResponse(this.RequestedContract.ContractId, this.petitionId, this.requestorFullJid, false));
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
            BindableProperty.Create(nameof(Created), typeof(DateTime), typeof(PetitionContractViewModel), default(DateTime));

        /// <summary>
        /// Created date of the contract
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
            BindableProperty.Create(nameof(Updated), typeof(DateTime?), typeof(PetitionContractViewModel), default(DateTime?));

        /// <summary>
        /// Updated date of the contract
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
            BindableProperty.Create(nameof(LegalId), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Legal id of the contract
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
            BindableProperty.Create(nameof(State), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Current state of the contract
        /// </summary>
        public string State
        {
            get => (string)this.GetValue(StateProperty);
            set => this.SetValue(StateProperty, value);
        }

        /// <summary>
        /// See <see cref="From"/>
        /// </summary>
        public static readonly BindableProperty FromProperty =
            BindableProperty.Create(nameof(From), typeof(DateTime?), typeof(PetitionContractViewModel), default(DateTime?));

        /// <summary>
        /// From date (validity range) of the contract
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
            BindableProperty.Create(nameof(To), typeof(DateTime?), typeof(PetitionContractViewModel), default(DateTime?));

        /// <summary>
        /// To date (validity range) of the contract
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
            BindableProperty.Create(nameof(FirstName), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// First name of the contract
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
            BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Middle name(s) of the contract
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
            BindableProperty.Create(nameof(LastNames), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Last name(s) of the contract
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
            BindableProperty.Create(nameof(PersonalNumber), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Personal number of the contract
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
            BindableProperty.Create(nameof(Address), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Address of the contract
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
            BindableProperty.Create(nameof(Address2), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Address (line 2) of the contract
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
            BindableProperty.Create(nameof(ZipCode), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Zip code of the contract
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
            BindableProperty.Create(nameof(Area), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Area of the contract
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
            BindableProperty.Create(nameof(City), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// City of the contract
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
            BindableProperty.Create(nameof(Region), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Region of the contract
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
            BindableProperty.Create(nameof(CountryCode), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Country code of the contract
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
            BindableProperty.Create(nameof(Country), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// Country of the contract
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
            BindableProperty.Create(nameof(PhoneNr), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// PhoneNr of the contract
        /// </summary>
        public string PhoneNr
        {
            get => (string)this.GetValue(PhoneNrProperty);
            set => this.SetValue(PhoneNrProperty, value);
        }

        /// <summary>
        /// See <see cref="IsApproved"/>
        /// </summary>
        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create(nameof(IsApproved), typeof(bool), typeof(PetitionContractViewModel), default(bool));

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
            BindableProperty.Create(nameof(Purpose), typeof(string), typeof(PetitionContractViewModel), default(string));

        /// <summary>
        /// What's the purpose of the petition?
        /// </summary>
        public string Purpose
        {
            get => (string)this.GetValue(PurposeProperty);
            set => this.SetValue(PurposeProperty, value);
        }

        #endregion

        private void AssignProperties()
        {
            if (!(this.requestorIdentity is null))
            {
                this.Created = this.requestorIdentity.Created;
                this.Updated = this.requestorIdentity.Updated.GetDateOrNullIfMinValue();
                this.LegalId = this.requestorIdentity.Id;
                this.State = this.requestorIdentity.State.ToString();
                this.From = this.requestorIdentity.From.GetDateOrNullIfMinValue();
                this.To = this.requestorIdentity.To.GetDateOrNullIfMinValue();
                this.FirstName = this.requestorIdentity[Constants.XmppProperties.FirstName];
                this.MiddleNames = this.requestorIdentity[Constants.XmppProperties.MiddleName];
                this.LastNames = this.requestorIdentity[Constants.XmppProperties.LastName];
                this.PersonalNumber = this.requestorIdentity[Constants.XmppProperties.PersonalNumber];
                this.Address = this.requestorIdentity[Constants.XmppProperties.Address];
                this.Address2 = this.requestorIdentity[Constants.XmppProperties.Address2];
                this.ZipCode = this.requestorIdentity[Constants.XmppProperties.ZipCode];
                this.Area = this.requestorIdentity[Constants.XmppProperties.Area];
                this.City = this.requestorIdentity[Constants.XmppProperties.City];
                this.Region = this.requestorIdentity[Constants.XmppProperties.Region];
                this.CountryCode = this.requestorIdentity[Constants.XmppProperties.Country];
                this.Country = ISO_3166_1.ToName(this.CountryCode);
                this.PhoneNr = this.requestorIdentity[Constants.XmppProperties.Phone];
                this.IsApproved = this.requestorIdentity.State == IdentityState.Approved;
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
                this.PhoneNr = Constants.NotAvailableValue;
                this.IsApproved = false;
            }
            this.Purpose = this.purpose;
        }

    }
}