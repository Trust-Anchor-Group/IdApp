using System.Collections.Generic;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Models
{
    /// <summary>
    /// The data model for registering an identity.
    /// </summary>
    public class RegisterIdentityModel
    {
        public string FirstName { get; set; }
        public string MiddleNames { get; set; }
        public string LastNames { get; set; }
        public string PersonalNumber { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string ZipCode { get; set; }
        public string Area { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string DeviceId { get; set; }
        public string JId { get; set; }

        /// <summary>
        /// Converts the <see cref="RegisterIdentityModel"/> to an array of <inheritdoc cref="Property"/>.
        /// </summary>
        /// <param name="neuronService">The Neuron service to use for accessing the Bare Jid.</param>
        /// <returns>The <see cref="RegisterIdentityModel"/> as a list of properties.</returns>
        public Property[] ToProperties(INeuronService neuronService)
        {
            List<Property> properties = new List<Property>();
            string s;

            if (!string.IsNullOrWhiteSpace(s = this.FirstName?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.FirstName, s));

            if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.MiddleName, s));

            if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.LastName, s));

            if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.PersonalNumber, s));

            if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Address, s));

            if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Address2, s));

            if (!string.IsNullOrWhiteSpace(s = this.ZipCode?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.ZipCode, s));

            if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Area, s));

            if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.City, s));

            if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Region, s));

            if (!string.IsNullOrWhiteSpace(s = this.Country?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.Country, ISO_3166_1.ToCode(s)));

            if (!string.IsNullOrWhiteSpace(s = this.DeviceId?.Trim()))
                properties.Add(new Property(Constants.XmppProperties.DeviceId, s));

            properties.Add(new Property(Constants.XmppProperties.JId, neuronService.BareJId));

            return properties.ToArray();

        }
    }
}