using IdApp.Navigation;
using IdApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.ViewModels.Contracts
{
    /// <summary>
    /// The view model to bind to when displaying petitioning of an identity in a view or page.
    /// </summary>
    public class PetitionIdentityViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        private readonly INeuronService neuronService;
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private LegalIdentity requestorIdentity;
        private string requestorFullJid;
        private string requestedIdentityId;
        private string petitionId;
        private string purpose;
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="PetitionIdentityViewModel"/> class.
        /// </summary>
        public PetitionIdentityViewModel()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.AcceptCommand = new Command(async _ => await Accept());
            this.DeclineCommand = new Command(async _ => await Decline());
            this.IgnoreCommand = new Command(async _ => await Ignore());
            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.neuronService, DependencyService.Resolve<IImageCacheService>(), this.Photos);
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out PetitionIdentityNavigationArgs args))
            {
                this.requestorIdentity = args.RequestorIdentity;
                this.requestorFullJid = args.RequestorFullJid;
                this.requestedIdentityId = args.RequestedIdentityId;
                this.petitionId = args.PetitionId;
                this.purpose = args.Purpose;
            }
            this.AssignProperties();
            if (this.requestorIdentity?.Attachments != null)
            {
                _ = this.photosLoader.LoadPhotos(this.requestorIdentity.Attachments);
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
        /// The list of photos related to the identity being petitioned.
        /// </summary>
        public ObservableCollection<ImageSource> Photos { get; }

        private async Task Accept()
        {
            bool succeeded = await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionIdentityResponse(this.requestedIdentityId, this.petitionId, this.requestorFullJid, true));
            if (succeeded)
            {
                await this.navigationService.GoBackAsync();
            }
        }

        private async Task Decline()
        {
            bool succeeded = await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionIdentityResponse(this.requestedIdentityId, this.petitionId, this.requestorFullJid, false));
            if (succeeded)
            {
                await this.navigationService.GoBackAsync();
            }
        }

        private async Task Ignore()
        {
            await this.navigationService.GoBackAsync();
        }

        #region Properties

        /// <summary>
        /// See <see cref="Created"/>
        /// </summary>
        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create("Created", typeof(DateTime), typeof(PetitionIdentityViewModel), default(DateTime));

        /// <summary>
        /// Created date of the identity
        /// </summary>
        public DateTime Created
        {
            get { return (DateTime)GetValue(CreatedProperty); }
            set { SetValue(CreatedProperty, value); }
        }

        /// <summary>
        /// See <see cref="Updated"/>
        /// </summary>
        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create("Updated", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// Updated date of the identity
        /// </summary>
        public DateTime? Updated
        {
            get { return (DateTime?)GetValue(UpdatedProperty); }
            set { SetValue(UpdatedProperty, value); }
        }

        /// <summary>
        /// See <see cref="LegalId"/>
        /// </summary>
        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Legal id of the identity
        /// </summary>
        public string LegalId
        {
            get { return (string)GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        /// <summary>
        /// See <see cref="State"/>
        /// </summary>
        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Current state of the identity
        /// </summary>
        public string State
        {
            get { return (string)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        /// <summary>
        /// See <see cref="From"/>
        /// </summary>
        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// From date (validity range) of the identity
        /// </summary>
        public DateTime? From
        {
            get { return (DateTime?)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        /// <summary>
        /// See <see cref="To"/>
        /// </summary>
        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        /// <summary>
        /// To date (validity range) of the identity
        /// </summary>
        public DateTime? To
        {
            get { return (DateTime?)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        /// <summary>
        /// See <see cref="FirstName"/>
        /// </summary>
        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// First name of the identity
        /// </summary>
        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        /// <summary>
        /// See <see cref="MiddleNames"/>
        /// </summary>
        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Middle name(s) of the identity
        /// </summary>
        public string MiddleNames
        {
            get { return (string)GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        /// <summary>
        /// See <see cref="LastNames"/>
        /// </summary>
        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Last name(s) of the identity
        /// </summary>
        public string LastNames
        {
            get { return (string)GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        /// <summary>
        /// See <see cref="PersonalNumber"/>
        /// </summary>
        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Personal number of the identity
        /// </summary>
        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        /// <summary>
        /// See <see cref="Address"/>
        /// </summary>
        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Address of the identity
        /// </summary>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        /// <summary>
        /// See <see cref="Address2"/>
        /// </summary>
        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Address (line 2) of the identity
        /// </summary>
        public string Address2
        {
            get { return (string)GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        /// <summary>
        /// See <see cref="ZipCode"/>
        /// </summary>
        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Zip code of the identity
        /// </summary>
        public string ZipCode
        {
            get { return (string)GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        /// <summary>
        /// See <see cref="Area"/>
        /// </summary>
        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Area of the identity
        /// </summary>
        public string Area
        {
            get { return (string)GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        /// <summary>
        /// See <see cref="City"/>
        /// </summary>
        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// City of the identity
        /// </summary>
        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        /// <summary>
        /// See <see cref="Region"/>
        /// </summary>
        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Region of the identity
        /// </summary>
        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        /// <summary>
        /// See <see cref="CountryCode"/>
        /// </summary>
        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Country code of the identity
        /// </summary>
        public string CountryCode
        {
            get { return (string)GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        /// <summary>
        /// See <see cref="Country"/>
        /// </summary>
        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// Country of the identity
        /// </summary>
        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        /// <summary>
        /// See <see cref="IsApproved"/>
        /// </summary>
        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(PetitionIdentityViewModel), default(bool));

        /// <summary>
        /// Is the contract approved?
        /// </summary>
        public bool IsApproved
        {
            get { return (bool)GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        /// <summary>
        /// See <see cref="Purpose"/>
        /// </summary>
        public static readonly BindableProperty PurposeProperty =
            BindableProperty.Create("Purpose", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        /// <summary>
        /// What's the purpose of the petition?
        /// </summary>
        public string Purpose
        {
            get { return (string)GetValue(PurposeProperty); }
            set { SetValue(PurposeProperty, value); }
        }

        #endregion

        private void AssignProperties()
        {
            if (this.requestorIdentity != null)
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
                this.IsApproved = this.requestorIdentity.State == IdentityState.Approved;
            }
            else
            {
                this.Created = DateTime.MinValue;
                this.Updated = null;
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
            }
            this.Purpose = this.purpose;
        }
    }
}