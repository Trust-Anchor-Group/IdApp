using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
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
            INavigationService navigationService,
            ISettingsService settingsService,
            IContractsService contractsService)
            : base(RegistrationStep.ValidateIdentity, tagProfile, neuronService, navigationService, settingsService)
        {
            this.contractsService = contractsService;
            this.navigationService = navigationService;
            this.InviteReviewerCommand = new Command(async _ => await InviteReviewer(), _ => this.State == IdentityState.Created);
            this.ContinueCommand = new Command(_ => OnStepCompleted(EventArgs.Empty), _ => IsApproved);
            this.Title = AppResources.ValidatingInformation;
            this.Photos = new ObservableCollection<ImageSource>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            AssignProperties();
            this.TagProfile.Changed += TagProfile_Changed;
            this.contractsService.LegalIdentityChanged += ContractsService_LegalIdentityChanged;
        }

        protected override async Task DoUnbind()
        {
            this.TagProfile.Changed -= TagProfile_Changed;
            this.contractsService.LegalIdentityChanged -= ContractsService_LegalIdentityChanged;
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

#pragma warning disable 4014
            LoadPhotos();
#pragma warning restore 4014
        }

        private async Task LoadPhotos()
        {
            this.Photos.Clear();

            if (!(this.TagProfile.LegalIdentity.Attachments is null))
            {

                foreach (Attachment attachment in this.TagProfile.LegalIdentity.Attachments.GetImageAttachments())
                {
                    try
                    {
                        KeyValuePair<string, TemporaryFile> pair = await this.contractsService.GetContractAttachmentAsync(attachment.Url, Constants.Timeouts.DownloadFile);

                        using (TemporaryFile file = pair.Value)
                        {
                            file.Reset();
                            MemoryStream ms = new MemoryStream();
                            await file.CopyToAsync(ms);
                            ms.Reset();
                            this.Photos.Add(ImageSource.FromStream(() => ms));
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void TagProfile_Changed(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(AssignProperties);
        }

        private void ContractsService_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                this.TagProfile.SetLegalIdentity(e.Identity);
                AssignProperties();
            });
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
            get { return (string)GetValue(LegalIdProperty); }
            set { SetValue(LegalIdProperty, value); }
        }

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

        private async Task InviteReviewer()
        {
            ScanQrCodePage page = new ScanQrCodePage();
            string code = await page.ScanQrCode();

            const string idScheme = Constants.IoTSchemes.IotId + ":";

            if (!string.IsNullOrWhiteSpace(code))
            {
                if (!code.StartsWith(idScheme, StringComparison.InvariantCultureIgnoreCase))
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity);
                    return;
                }

                await this.contractsService.PetitionPeerReviewIdAsync(code.Substring(idScheme.Length), this.TagProfile.LegalIdentity, Guid.NewGuid().ToString(), AppResources.CouldYouPleaseReviewMyIdentityInformation);

                await this.NavigationService.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToYourPeer);
            }
        }
    }
}