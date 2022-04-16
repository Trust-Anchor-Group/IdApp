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
				return this.name switch
				{
					"ALT" => AppResources.Altitude,
					"APT" => AppResources.Apartment,
					"AREA" => AppResources.Area,
					"BLD" => AppResources.Building,
					"CITY" => AppResources.City,
					"CLASS" => AppResources.Class,
					"COUNTRY" => AppResources.Country,
					"PHONE" => AppResources.Phone,
					"KEY" => AppResources.Key,
					"LAT" => AppResources.Latitude,
					"LON" => AppResources.Longitude,
					"MAN" => AppResources.Manufacturer,
					"MLOC" => AppResources.MeterLocation,
					"MNR" => AppResources.MeterNumber,
					"MODEL" => AppResources.Model,
					"NAME" => AppResources.Name,
					"PURL" => AppResources.ProductInformation,
					"R" => AppResources.Registry,
					"REGION" => AppResources.Region,
					"ROOM" => AppResources.Room,
					"SN" => AppResources.SerialNumber,
					"STREET" => AppResources.StreetName,
					"STREETNR" => AppResources.StreetNumber,
					"V" => AppResources.Version,
					_ => this.name,
				};
			}
		}

		/// <summary>
		/// Unit associated with the tag.
		/// </summary>
		public string Unit
		{
			get
			{
				return this.name switch
				{
					"ALT" => "m",
					"LAT" => "°",
					"LON" => "°",
					_ => string.Empty,
				};
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
