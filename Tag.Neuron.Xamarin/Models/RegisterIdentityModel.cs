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
        /// <summary>
        /// First name
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Middle name(s) as one string
        /// </summary>
        public string MiddleNames { get; set; }
        /// <summary>
        /// Last name(s) as one string
        /// </summary>
        public string LastNames { get; set; }
        /// <summary>
        /// Personal number
        /// </summary>
        public string PersonalNumber { get; set; }
        /// <summary>
        /// Address, line 1
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Address, line 2
        /// </summary>
        public string Address2 { get; set; }
        /// <summary>
        /// Zip code (postal code)
        /// </summary>
        public string ZipCode { get; set; }
        /// <summary>
        /// Area
        /// </summary>
        public string Area { get; set; }
        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Region
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Device Id
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// Jabber Id
        /// </summary>
        public string Jid { get; set; }

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

            properties.Add(new Property(Constants.XmppProperties.Jid, neuronService.BareJid));

            return properties.ToArray();

        }
    }
}