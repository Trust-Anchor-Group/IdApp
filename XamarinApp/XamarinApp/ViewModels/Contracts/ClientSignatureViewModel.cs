using System;
using System.Globalization;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Extensions;

namespace XamarinApp.ViewModels.Contracts
{
    public class ClientSignatureViewModel : BaseViewModel
    {
        public ClientSignatureViewModel(
            ClientSignature clientSignature,
            LegalIdentity identity)
        {
            AssignProperties(clientSignature, identity);
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

        public static readonly BindableProperty RoleProperty =
            BindableProperty.Create("Role", typeof(string), typeof(ClientSignatureViewModel), default(string));

        public string Role
        {
            get { return (string) GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create("Timestamp", typeof(string), typeof(ClientSignatureViewModel), default(string));

        public string Timestamp
        {
            get { return (string) GetValue(TimestampProperty); }
            set { SetValue(TimestampProperty, value); }
        }

        public static readonly BindableProperty IsTransferableProperty =
            BindableProperty.Create("IsTransferable", typeof(string), typeof(ClientSignatureViewModel), default(string));

        public string IsTransferable
        {
            get { return (string) GetValue(IsTransferableProperty); }
            set { SetValue(IsTransferableProperty, value); }
        }

        public static readonly BindableProperty BareJIdProperty =
            BindableProperty.Create("BareJId", typeof(string), typeof(ClientSignatureViewModel), default(string));

        public string BareJId
        {
            get { return (string) GetValue(BareJIdProperty); }
            set { SetValue(BareJIdProperty, value); }
        }

        public static readonly BindableProperty SignatureProperty =
            BindableProperty.Create("Signature", typeof(string), typeof(ClientSignatureViewModel), default(string));

        public string Signature
        {
            get { return (string) GetValue(SignatureProperty); }
            set { SetValue(SignatureProperty, value); }
        }

        #endregion

        private void AssignProperties(ClientSignature signature, LegalIdentity identity)
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
            this.Role = signature.Role;
            this.Timestamp = signature.Timestamp.ToString(CultureInfo.CurrentUICulture);
            this.IsTransferable = signature.Transferable ? AppResources.Yes : AppResources.No;
            this.BareJId = signature.BareJid;
            this.Signature = Convert.ToBase64String(signature.DigitalSignature);
        }
    }
}