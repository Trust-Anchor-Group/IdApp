using System;
using System.Windows.Input;
using Waher.Networking.XMPP.Provisioning;
using Xamarin.Forms;

namespace IdApp.ViewModels.Things
{
	/// <summary>
	/// Classed used to present a meta-data tag in a human interface.
	/// </summary>
	public class HumanReadableTag
	{
		private readonly MetaDataTag tag;
		private readonly ViewClaimThingViewModel model;

		/// <summary>
		/// Classed used to present a meta-data tag in a human interface.
		/// </summary>
		/// <param name="Tag">Meta-data tag.</param>
		/// <param name="Model">View model reference.</param>
		public HumanReadableTag(MetaDataTag Tag, ViewClaimThingViewModel Model)
		{
			this.tag = Tag;
			this.model = Model;

            this.ClickCommand = new Command(_ => this.LabelClicked());
		}

		/// <summary>
		/// The command for clicking a tag value.
		/// </summary>
		public ICommand ClickCommand { get; }

		/// <summary>
		/// Human-readable tag name
		/// </summary>
		public string Name
		{
			get
			{
				switch (this.tag.Name)
				{
					case "ALT": return AppResources.Altitude;
					case "APT": return AppResources.Apartment;
					case "AREA": return AppResources.Area;
					case "BLD": return AppResources.Building;
					case "CITY": return AppResources.City;
					case "CLASS": return AppResources.Class;
					case "COUNTRY": return AppResources.Country;
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
					default: return this.tag.Name;
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
				switch (this.tag.Name)
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
		public string StringValue
		{
			get
			{
				string s = this.Unit;

				if (string.IsNullOrEmpty(s))
					return this.tag.StringValue;
				else
					return this.tag.StringValue + " " + s;
			}
		}

		/// <summary>
		/// Called when tag value has been clicked.
		/// </summary>
		private void LabelClicked()
		{
			this.model?.LabelClicked(this);
		}

	}
}
