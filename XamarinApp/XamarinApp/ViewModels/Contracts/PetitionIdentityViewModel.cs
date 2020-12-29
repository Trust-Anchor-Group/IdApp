﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Extensions;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace XamarinApp.ViewModels.Contracts
{
    public class PetitionIdentityViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        private readonly INeuronService neuronService;
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private readonly LegalIdentity requestorIdentity;
        private readonly string requestorFullJid;
        private readonly string requestedIdentityId;
        private readonly string petitionId;
        private readonly PhotosLoader photosLoader;

        public PetitionIdentityViewModel(
            LegalIdentity requestorIdentity,
            string requestorFullJid,
            string requestedIdentityId,
            string petitionId,
            string purpose)
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.requestorIdentity = requestorIdentity;
            this.requestorFullJid = requestorFullJid;
            this.requestedIdentityId = requestedIdentityId;
            this.petitionId = petitionId;
            this.AcceptCommand = new Command(async _ => await Accept());
            this.DeclineCommand = new Command(async _ => await Decline());
            this.IgnoreCommand = new Command(async _ => await Ignore());
            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.neuronService);
            AssignProperties(purpose);
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.requestorIdentity?.Attachments != null)
            {
                _ = this.photosLoader.LoadPhotos(this.requestorIdentity.Attachments, this.Photos);
            }
        }

        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            this.Photos.Clear();
            await base.DoUnbind();
        }

        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }
        public ICommand IgnoreCommand { get; }

        public ObservableCollection<ImageSource> Photos { get; }

        private async Task Accept()
        {
            bool succeeded = await this.networkService.Request(this.neuronService.Contracts.SendPetitionIdentityResponseAsync, this.requestedIdentityId, this.petitionId, this.requestorFullJid, true);
            if (succeeded)
            {
                await this.navigationService.PopAsync();
            }
        }

        private async Task Decline()
        {
            bool succeeded = await this.networkService.Request(this.neuronService.Contracts.SendPetitionIdentityResponseAsync, this.requestedIdentityId, this.petitionId, this.requestorFullJid, false);
            if (succeeded)
            {
                await this.navigationService.PopAsync();
            }
        }

        private async Task Ignore()
        {
            await this.navigationService.PopAsync();
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

        private void AssignProperties(string purpose)
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
            this.Purpose = purpose;
        }

    }
}