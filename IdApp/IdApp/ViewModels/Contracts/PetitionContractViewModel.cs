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
    public class PetitionContractViewModel : BaseViewModel
    {
        private readonly INeuronService neuronService;
        private readonly INavigationService navigationService;
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private LegalIdentity requestorIdentity;
        private Contract requestedContract;
        private string requestorFullJid;
        private string petitionId;
        private string purpose;
        private readonly PhotosLoader photosLoader;

        public PetitionContractViewModel()
        {
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.AcceptCommand = new Command(async _ => await Accept());
            this.DeclineCommand = new Command(async _ => await Decline());
            this.IgnoreCommand = new Command(async _ => await Ignore());
            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.neuronService, DependencyService.Resolve<IImageCacheService>(), this.Photos);
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out PetitionContractNavigationArgs args))
            {
                this.requestorIdentity = args.RequestorIdentity;
                this.requestorFullJid = args.RequestorFullJid;
                this.requestedContract = args.RequestedContract;
                this.petitionId = args.PetitionId;
                this.purpose = args.Purpose;
            }
            AssignProperties();
            if (this.requestorIdentity?.Attachments != null)
            {
                _ = this.photosLoader.LoadPhotos(this.requestorIdentity.Attachments);
            }
        }

        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            await base.DoUnbind();
        }

        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }
        public ICommand IgnoreCommand { get; }

        public ObservableCollection<ImageSource> Photos { get; }

        private async Task Accept()
        {
            bool succeeded = await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionContractResponse(this.requestedContract.ContractId, this.petitionId, this.requestorFullJid, true));
            if (succeeded)
            {
                await this.navigationService.GoBackAsync();
            }
        }

        private async Task Decline()
        {
            bool succeeded = await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionIdentityResponse(this.requestedContract.ContractId, this.petitionId, this.requestorFullJid, false));
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

        public static readonly BindableProperty CreatedProperty =
            BindableProperty.Create("Created", typeof(DateTime), typeof(PetitionIdentityViewModel), default(DateTime));

        public DateTime Created
        {
            get { return (DateTime) GetValue(CreatedProperty); }
            set { SetValue(CreatedProperty, value); }
        }

        public static readonly BindableProperty UpdatedProperty =
            BindableProperty.Create("Updated", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        public DateTime? Updated
        {
            get { return (DateTime?) GetValue(UpdatedProperty); }
            set { SetValue(UpdatedProperty, value); }
        }

        public static readonly BindableProperty LegalIdProperty =
            BindableProperty.Create("LegalId", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string LegalId
        {
            get { return (string) GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string State
        {
            get { return (string) GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        public DateTime? From
        {
            get { return (DateTime?) GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(PetitionIdentityViewModel), default(DateTime?));

        public DateTime? To
        {
            get { return (DateTime?) GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string FirstName
        {
            get { return (string) GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string MiddleNames
        {
            get { return (string) GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string LastNames
        {
            get { return (string) GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string PersonalNumber
        {
            get { return (string) GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string Address
        {
            get { return (string) GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string Address2
        {
            get { return (string) GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string ZipCode
        {
            get { return (string) GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string Area
        {
            get { return (string) GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string City
        {
            get { return (string) GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string Region
        {
            get { return (string) GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string CountryCode
        {
            get { return (string) GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string Country
        {
            get { return (string) GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(PetitionIdentityViewModel), default(bool));

        public bool IsApproved
        {
            get { return (bool) GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        public static readonly BindableProperty PurposeProperty =
            BindableProperty.Create("Purpose", typeof(string), typeof(PetitionIdentityViewModel), default(string));

        public string Purpose
        {
            get { return (string) GetValue(PurposeProperty); }
            set { SetValue(PurposeProperty, value); }
        }

        #endregion

        private void AssignProperties()
        {
            if (this.requestorIdentity != null)
            {
                this.Created = requestorIdentity.Created;
                this.Updated = requestorIdentity.Updated.GetDateOrNullIfMinValue();
                this.LegalId = requestorIdentity.Id;
                this.State = requestorIdentity.State.ToString();
                this.From = requestorIdentity.From.GetDateOrNullIfMinValue();
                this.To = requestorIdentity.To.GetDateOrNullIfMinValue();
                this.FirstName = requestorIdentity[Constants.XmppProperties.FirstName];
                this.MiddleNames = requestorIdentity[Constants.XmppProperties.MiddleName];
                this.LastNames = requestorIdentity[Constants.XmppProperties.LastName];
                this.PersonalNumber = requestorIdentity[Constants.XmppProperties.PersonalNumber];
                this.Address = requestorIdentity[Constants.XmppProperties.Address];
                this.Address2 = requestorIdentity[Constants.XmppProperties.Address2];
                this.ZipCode = requestorIdentity[Constants.XmppProperties.ZipCode];
                this.Area = requestorIdentity[Constants.XmppProperties.Area];
                this.City = requestorIdentity[Constants.XmppProperties.City];
                this.Region = requestorIdentity[Constants.XmppProperties.Region];
                this.CountryCode = requestorIdentity[Constants.XmppProperties.Country];
                this.Country = ISO_3166_1.ToName(this.CountryCode);
                this.IsApproved = requestorIdentity.State == IdentityState.Approved;
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
            }
            this.Purpose = this.purpose;
        }

    }
}