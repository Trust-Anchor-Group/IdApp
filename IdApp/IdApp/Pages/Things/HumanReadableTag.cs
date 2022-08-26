using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Xamarin.CommunityToolkit.Helpers;

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
					"ALT" => LocalizationResourceManager.Current["Altitude"],
					"APT" => LocalizationResourceManager.Current["Apartment"],
					"AREA" => LocalizationResourceManager.Current["Area"],
					"BLD" => LocalizationResourceManager.Current["Building"],
					"CITY" => LocalizationResourceManager.Current["City"],
					"CLASS" => LocalizationResourceManager.Current["Class"],
					"COUNTRY" => LocalizationResourceManager.Current["Country"],
					"PHONE" => LocalizationResourceManager.Current["Phone"],
					"KEY" => LocalizationResourceManager.Current["Key"],
					"LAT" => LocalizationResourceManager.Current["Latitude"],
					"LON" => LocalizationResourceManager.Current["Longitude"],
					"MAN" => LocalizationResourceManager.Current["Manufacturer"],
					"MLOC" => LocalizationResourceManager.Current["MeterLocation"],
					"MNR" => LocalizationResourceManager.Current["MeterNumber"],
					"MODEL" => LocalizationResourceManager.Current["Model"],
					"NAME" => LocalizationResourceManager.Current["Name"],
					"PURL" => LocalizationResourceManager.Current["ProductInformation"],
					"R" => LocalizationResourceManager.Current["Registry"],
					"REGION" => LocalizationResourceManager.Current["Region"],
					"ROOM" => LocalizationResourceManager.Current["Room"],
					"SN" => LocalizationResourceManager.Current["SerialNumber"],
					"STREET" => LocalizationResourceManager.Current["StreetName"],
					"STREETNR" => LocalizationResourceManager.Current["StreetNumber"],
					"V" => LocalizationResourceManager.Current["Version"],
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
