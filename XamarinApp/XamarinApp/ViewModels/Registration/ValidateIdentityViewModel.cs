using System;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ValidateIdentityViewModel : RegistrationStepViewModel
    {
        public ValidateIdentityViewModel(RegistrationStep step, TagProfile tagProfile, INeuronService neuronService,
            IMessageService messageService)
            : base(step, tagProfile, neuronService, messageService)
        {

        }

        public DateTime Created => this.TagProfile.LegalIdentity.Created;
        public DateTime? Updated => CheckMin(this.TagProfile.LegalIdentity.Updated);
        public string LegalId => this.TagProfile.LegalIdentity.Id;
        public string BareJid => this.NeuronService?.BareJId ?? string.Empty;
        public string State => this.TagProfile.LegalIdentity.State.ToString();
        public DateTime? From => CheckMin(this.TagProfile.LegalIdentity.From);
        public DateTime? To => CheckMin(this.TagProfile.LegalIdentity.To);
        public string FirstName => this.TagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
        public string MiddleNames => this.TagProfile.LegalIdentity[Constants.XmppProperties.MiddleName];
        public string LastNames => this.TagProfile.LegalIdentity[Constants.XmppProperties.LastName];
        public string PNr => this.TagProfile.LegalIdentity[Constants.XmppProperties.PersonalNumber];
        public string Address => this.TagProfile.LegalIdentity[Constants.XmppProperties.Address];
        public string Address2 => this.TagProfile.LegalIdentity[Constants.XmppProperties.Address2];
        public string PostalCode => this.TagProfile.LegalIdentity[Constants.XmppProperties.ZipCode];
        public string Area => this.TagProfile.LegalIdentity[Constants.XmppProperties.Area];
        public string City => this.TagProfile.LegalIdentity[Constants.XmppProperties.City];
        public string Region => this.TagProfile.LegalIdentity[Constants.XmppProperties.Region];
        public string CountryCode => this.TagProfile.LegalIdentity[Constants.XmppProperties.Country];
        public string Country => ISO_3166_1.ToName(this.CountryCode);
        public bool IsApproved => this.TagProfile.LegalIdentity.State == IdentityState.Approved;
        public bool IsCreated => this.TagProfile.LegalIdentity.State == IdentityState.Created;

        private static DateTime? CheckMin(DateTime? date)
        {
            if (!date.HasValue || date.Value == DateTime.MinValue)
                return null;
            return date;
        }
    }
}