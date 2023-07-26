using System.Collections.Generic;
using IdApp.Services.Data.Countries;
using IdApp.Services.Xmpp;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Registration.RegisterIdentity
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
		/// Organization name
		/// </summary>
		public string OrgName { get; set; }

		/// <summary>
		/// Organization number
		/// </summary>
		public string OrgNumber { get; set; }

		/// <summary>
		/// Organization Address, line 1
		/// </summary>
		public string OrgAddress { get; set; }

		/// <summary>
		/// Organization Address, line 2
		/// </summary>
		public string OrgAddress2 { get; set; }

		/// <summary>
		/// Organization Zip code (postal code)
		/// </summary>
		public string OrgZipCode { get; set; }

		/// <summary>
		/// Organization Area
		/// </summary>
		public string OrgArea { get; set; }

		/// <summary>
		/// Organization City
		/// </summary>
		public string OrgCity { get; set; }

		/// <summary>
		/// Organization Region
		/// </summary>
		public string OrgRegion { get; set; }

		/// <summary>
		/// Organization Country
		/// </summary>
		public string OrgCountry { get; set; }

		/// <summary>
		/// Organization Department
		/// </summary>
		public string OrgDepartment { get; set; }

		/// <summary>
		/// Organization Role
		/// </summary>
		public string OrgRole { get; set; }

		/// <summary>
		/// Phone Number
		/// </summary>
		public string PhoneNr { get; set; }

		/// <summary>
		/// EMail
		/// </summary>
		public string EMail { get; set; }

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
		/// <param name="XmppService">The XMPP service to use for accessing the Bare Jid.</param>
		/// <returns>The <see cref="RegisterIdentityModel"/> as a list of properties.</returns>
		public Property[] ToProperties(IXmppService XmppService)
		{
			List<Property> properties = new();
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

			if (!string.IsNullOrWhiteSpace(s = this.OrgName?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgName, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgNumber?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgNumber, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgDepartment?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgDepartment, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgRole?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgRole, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgAddress?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgAddress, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgAddress2?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgAddress2, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgZipCode?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgZipCode, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgArea?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgArea, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgCity?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgCity, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgRegion?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgRegion, s));

			if (!string.IsNullOrWhiteSpace(s = this.OrgCountry?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.OrgCountry, ISO_3166_1.ToCode(s)));

			if (!string.IsNullOrWhiteSpace(s = this.PhoneNr?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.Phone, s));

			if (!string.IsNullOrWhiteSpace(s = this.EMail?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.EMail, s));

			if (!string.IsNullOrWhiteSpace(s = this.DeviceId?.Trim()))
				properties.Add(new Property(Constants.XmppProperties.DeviceId, s));

			properties.Add(new Property(Constants.XmppProperties.Jid, XmppService.BareJid));

			return properties.ToArray();
		}
	}
}
