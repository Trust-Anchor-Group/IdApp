using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;
using XamarinApp.Views;

namespace XamarinApp.ViewModels.Registration
{
    public class ValidateIdentityViewModel : RegistrationStepViewModel
    {
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;

        public ValidateIdentityViewModel(
            TagProfile tagProfile, 
            INeuronService neuronService,
            IContractsService contractsService,
            INavigationService navigationService)
            : base(RegistrationStep.ValidateIdentity, tagProfile, neuronService, navigationService)
        {
            this.contractsService = contractsService;
            this.navigationService = navigationService;
            this.InviteReviewerCommand = new Command(async _ => await InviteReviewer(), _ => this.State == IdentityState.Created);
            this.ContinueCommand = new Command(_ => OnStepCompleted(EventArgs.Empty), _ => IsApproved);
            this.Title = AppResources.ValidatingInformation;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.TagProfile.Changed += TagProfile_Changed;
            this.contractsService.LegalIdentityChanged += ContractsService_LegalIdentityChanged;
        }

        protected override async Task DoUnbind()
        {
            this.TagProfile.Changed -= TagProfile_Changed;
            this.contractsService.LegalIdentityChanged -= ContractsService_LegalIdentityChanged;
            await base.DoUnbind();
        }


        public ICommand InviteReviewerCommand { get; }
        public ICommand ContinueCommand { get; }

        private void AssignProperties()
        {
            Created = this.TagProfile.LegalIdentity?.Created ?? DateTime.MinValue;
            Updated = this.TagProfile.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
            LegalId = this.TagProfile.LegalIdentity?.Id;
            BareJId = this.NeuronService?.BareJId ?? string.Empty;
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
        }

        private void TagProfile_Changed(object sender, EventArgs e)
        {
            AssignProperties();
        }

        private void ContractsService_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
        {
            this.TagProfile.SetLegalIdentity(e.Identity);
            AssignProperties();
        }

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
            get { return (string) GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

        public static readonly BindableProperty BareJIdProperty =
            BindableProperty.Create("BareJId", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string BareJId
        {
            get { return (string) GetValue(BareJIdProperty); }
            set { SetValue(BareJIdProperty, value); }
        }

        public static readonly BindableProperty StateProperty =
            BindableProperty.Create("State", typeof(IdentityState), typeof(ValidateIdentityViewModel), default(IdentityState));

        public IdentityState State
        {
            get { return (IdentityState) GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly BindableProperty FromProperty =
            BindableProperty.Create("From", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        public DateTime? From
        {
            get { return (DateTime?) GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public static readonly BindableProperty ToProperty =
            BindableProperty.Create("To", typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

        public DateTime? To
        {
            get { return (DateTime?) GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string FirstName
        {
            get { return (string) GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string MiddleNames
        {
            get { return (string) GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string LastNames
        {
            get { return (string) GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string PersonalNumber
        {
            get { return (string) GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Address
        {
            get { return (string) GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Address2
        {
            get { return (string) GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string ZipCode
        {
            get { return (string) GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Area
        {
            get { return (string) GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string City
        {
            get { return (string) GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Region
        {
            get { return (string) GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string Country
        {
            get { return (string) GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public static readonly BindableProperty CountryCodeProperty =
            BindableProperty.Create("CountryCode", typeof(string), typeof(ValidateIdentityViewModel), default(string));

        public string CountryCode
        {
            get { return (string) GetValue(CountryCodeProperty); }
            set { SetValue(CountryCodeProperty, value); }
        }

        public static readonly BindableProperty IsApprovedProperty =
            BindableProperty.Create("IsApproved", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        public bool IsApproved
        {
            get { return (bool) GetValue(IsApprovedProperty); }
            set { SetValue(IsApprovedProperty, value); }
        }

        public static readonly BindableProperty IsCreatedProperty =
            BindableProperty.Create("IsCreated", typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

        public bool IsCreated
        {
            get { return (bool) GetValue(IsCreatedProperty); }
            set { SetValue(IsCreatedProperty, value); }
        }

        private ICommand CodeScannedCommand { get; set; }

        TaskCompletionSource<bool> qrCodeScanned;

        private Task ScanQrCode()
        {
            qrCodeScanned = new TaskCompletionSource<bool>();
            return qrCodeScanned.Task;
        }

        private void AddPartViewModel_QrCodeScanned(object sender, EventArgs e)
        {
            if (qrCodeScanned != null)
            {
                qrCodeScanned.TrySetResult(true);
                qrCodeScanned = null;
            }   
        }

        private async Task InviteReviewer()
        {
            // 1. Create a default page
            ContentBasePage page = new ContentBasePage();
            AddPartViewModel viewModel = new AddPartViewModel();
            AddPartView view = new AddPartView(viewModel);
            // 2. Set it's content to the 'Scan QR Code View'
            page.Content = view;
            viewModel.CodeScanned += AddPartViewModel_QrCodeScanned;
            // 3. Show it as a modal page
            await this.navigationService.PushModalAsync(page);
            // 4. Wait for the scan to complete
            await ScanQrCode();
            await this.navigationService.PopModalAsync();
            viewModel.CodeScanned -= AddPartViewModel_QrCodeScanned;

            if (!string.IsNullOrWhiteSpace(viewModel.Code))
            {
                if (!viewModel.Code.StartsWith(Constants.Schemes.IotId + ":", StringComparison.InvariantCultureIgnoreCase))
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity);
                }

                await this.contractsService.PetitionPeerReviewIdAsync(viewModel.Code.Substring(6), this.TagProfile.LegalIdentity, Guid.NewGuid().ToString(), "Could you please review my identity information?");

                await this.NavigationService.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToYourPeer);
            }
        }
    }
}