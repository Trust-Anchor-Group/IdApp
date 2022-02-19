using IdApp.Resx;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;

namespace IdApp.Pages.Things
{
	/// <summary>
	/// Class used to present a meta-data tag in a human interface.
	/// </summary>
	public class HumanReadableTag
	{
		private readonly string name;
		private readonly string value;

		/// <summary>
		/// Classed used to present a meta-data tag in a human interface.
		/// </summary>
		/// <param name="Tag">Meta-data tag.</param>
		public HumanReadableTag(MetaDataTag Tag)
		{
			this.name = Tag.Name;
			this.value = Tag.StringValue;
		}

		/// <summary>
		/// Classed used to present a meta-data tag in a human interface.
		/// </summary>
		/// <param name="Tag">Meta-data tag.</param>
		public HumanReadableTag(Property Tag)
		{
			this.name = Tag.Name;
			this.value = Tag.Value;
		}

		/// <summary>
		/// Tag name.
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// Tag value.
		/// </summary>
		public string Value => this.value;

		/// <summary>
		/// Human-readable tag name
		/// </summary>
		public string LocalizedName
		{
			get
			{
				switch (this.name)
				{
					case "ALT": return AppResources.Altitude;
					case "APT": return AppResources.Apartment;
					case "AREA": return AppResources.Area;
					case "BLD": return AppResources.Building;
					case "CITY": return AppResources.City;
					case "CLASS": return AppResources.Class;
					case "COUNTRY": return AppResources.Country;
					case "PHONE": return AppResources.Phone;
					case "KEY": return AppResources.Key;
					case "LAT": return AppResources.Latitude;
					case "LON": return AppResources.Longitude;
					case "MAN": return AppResources.Manufacturer;
					case "MLOC": return AppResources.MeterLocation;
					case "MNR": return AppResources.MeterNumber;
					case "MODEL": return AppResources.Model;
					case "NAME": return AppResources.Name;
					case "PURL": return AppResources.ProductInformation;
					case "R": return AppResources.Registry;
					case "REGION": return AppResources.Region;
					case "ROOM": return AppResources.Room;
					case "SN": return AppResources.SerialNumber;
					case "STREET": return AppResources.StreetName;
					case "STREETNR": return AppResources.StreetNumber;
					case "V": return AppResources.Version;
					default: return this.name;
				}
			}
		}

		/// <summary>
		/// Unit associated with the tag.
		/// </summary>
		public string Unit
		{
			get
			{
				switch (this.name)
				{
					case "ALT": return "m";
					case "LAT": return "°";
					case "LON": return "°";
					default: return string.Empty;
				}
			}
		}

		/// <summary>
		/// String value of tag.
		/// </summary>
		public string LocalizedValue
		{
			get
			{
				string s = this.Unit;

				if (string.IsNullOrEmpty(s))
					return this.value;
				else
					return this.value + " " + s;
			}
		}
	}
}
