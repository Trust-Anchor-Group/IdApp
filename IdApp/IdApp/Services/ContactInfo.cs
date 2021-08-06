using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Persistence.Filters;

namespace IdApp.Services
{
	/// <summary>
	/// Contains information about a contact.
	/// </summary>
	[CollectionName("ContactInformation")]
	[TypeName(TypeNameSerialization.None)]
	[Index("BareJid", "SourceId", "Partition", "NodeId")]
	[Index("IsThing", "BareJid", "SourceId", "Partition", "NodeId")]
	[Index("LegalId")]
	public class ContactInfo
	{
		private string objectId = null;
		private CaseInsensitiveString bareJid = CaseInsensitiveString.Empty;
		private CaseInsensitiveString legalId = CaseInsensitiveString.Empty;
		private LegalIdentity legalIdentity = null;
		private Property[] metaData = null;
		private string friendlyName = string.Empty;
		private string sourceId = string.Empty;
		private string partition = string.Empty;
		private string nodeId = string.Empty;
		private string registryJid = string.Empty;
		private bool? subcribeTo = null;
		private bool? allowSubscriptionFrom = null;
		private bool? isThing = null;
		private bool? owner = null;

		/// <summary>
		/// Contains information about a contact.
		/// </summary>
		public ContactInfo()
		{
		}

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string ObjectId
		{
			get => this.objectId;
			set => this.objectId = value;
		}

		/// <summary>
		/// Bare JID of contact.
		/// </summary>
		public string BareJid
		{
			get => this.bareJid;
			set => this.bareJid = value;
		}

		/// <summary>
		/// Legal ID of contact.
		/// </summary>
		public string LegalId
		{
			get => this.legalId;
			set => this.legalId = value;
		}

		/// <summary>
		/// Legal Identity object.
		/// </summary>
		[DefaultValueNull]
		public LegalIdentity LegalIdentity
		{
			get => this.legalIdentity;
			set => this.legalIdentity = value;
		}

		/// <summary>
		/// Friendly name.
		/// </summary>
		[DefaultValueStringEmpty]
		public string FriendlyName
		{
			get => this.friendlyName;
			set => this.friendlyName = value;
		}

		/// <summary>
		/// Source ID
		/// </summary>
		public string SourceId
		{
			get => this.sourceId;
			set => this.sourceId = value;
		}

		/// <summary>
		/// Partition
		/// </summary>
		public string Partition
		{
			get => this.partition;
			set => this.partition = value;
		}

		/// <summary>
		/// Node ID
		/// </summary>
		public string NodeId
		{
			get => this.nodeId;
			set => this.nodeId = value;
		}

		/// <summary>
		/// Registry JID
		/// </summary>
		public string RegistryJid
		{
			get => this.registryJid;
			set => this.registryJid = value;
		}

		/// <summary>
		/// Subscribe to this contact
		/// </summary>
		public bool? SubcribeTo
		{
			get => this.subcribeTo;
			set => this.subcribeTo = value;
		}

		/// <summary>
		/// Allow subscriptions from this contact
		/// </summary>
		public bool? AllowSubscriptionFrom
		{
			get => this.allowSubscriptionFrom;
			set => this.allowSubscriptionFrom = value;
		}

		/// <summary>
		/// The contact is a thing
		/// </summary>
		public bool? IsThing
		{
			get => this.isThing;
			set => this.isThing = value;
		}

		/// <summary>
		/// If the account is registered as the owner of the thing.
		/// </summary>
		[DefaultValueNull]
		public bool? Owner
		{
			get => this.owner;
			set => this.owner = value;
		}

		/// <summary>
		/// Meta-data related to a thing.
		/// </summary>
		[DefaultValueNull]
		public Property[] MetaData
		{
			get => this.metaData;
			set => this.metaData = value;
		}

		/// <summary>
		/// Finds information about a contact, given its Bare JID.
		/// </summary>
		/// <param name="BareJid">Bare JID</param>
		/// <returns>Contact information, if found.</returns>
		public static Task<ContactInfo> FindByBareJid(string BareJid)
		{
			return Database.FindFirstIgnoreRest<ContactInfo>(new FilterFieldEqualTo("BareJid", BareJid));
		}

		/// <summary>
		/// Finds information about a contact, given its Bare JID.
		/// </summary>
		/// <param name="BareJid">Bare JID</param>
		/// <param name="SourceId">Source ID</param>
		/// <param name="Partition">Partition</param>
		/// <param name="NodeId">Node ID</param>
		/// <returns>Contact information, if found.</returns>
		public static Task<ContactInfo> FindByBareJid(string BareJid, string SourceId, string Partition, string NodeId)
		{
			return Database.FindFirstIgnoreRest<ContactInfo>(new FilterAnd(
				new FilterFieldEqualTo("BareJid", BareJid),
				new FilterFieldEqualTo("SourceId", SourceId),
				new FilterFieldEqualTo("Partition", Partition),
				new FilterFieldEqualTo("NodeId", NodeId)));
		}

		/// <summary>
		/// Finds information about a contact, given its Legal ID.
		/// </summary>
		/// <param name="LegalId">Legal ID</param>
		/// <returns>Contact information, if found.</returns>
		public static Task<ContactInfo> FindByLegalId(string LegalId)
		{
			return Database.FindFirstIgnoreRest<ContactInfo>(new FilterFieldEqualTo("LegalId", LegalId));
		}

		/// <summary>
		/// Gets the friendly name of a remote identity (Legal ID or Bare JID).
		/// </summary>
		/// <param name="RemoteId">Remote Identity</param>
		/// <param name="Client">XMPP Client</param>
		/// <returns>Friendly name.</returns>
		public static async Task<string> GetFriendlyName(string RemoteId, XmppClient Client)
		{
			int i = RemoteId.IndexOf('@');
			if (i < 0)
				return RemoteId;

			string Account = RemoteId.Substring(0, i);
			ContactInfo Info;

			if (Guid.TryParse(Account, out Guid _))
			{
				Info = await FindByLegalId(RemoteId);
				if (!string.IsNullOrEmpty(Info?.FriendlyName))
					return Info.FriendlyName;

				if (!(Info?.LegalIdentity is null))
					return GetFriendlyName(Info.LegalIdentity);
			}

			Info = await FindByBareJid(RemoteId);
			if (!(Info is null))
			{
				if (!string.IsNullOrEmpty(Info?.FriendlyName))
					return Info.FriendlyName;

				if (!(Info.LegalIdentity is null))
					return GetFriendlyName(Info.LegalIdentity);

				if (!(Info.MetaData is null))
				{
					string s = GetFriendlyName(Info.MetaData);
					if (!string.IsNullOrEmpty(s))
						return s;
				}
			}

			RosterItem Item = Client?.GetRosterItem(RemoteId);
			if (!(Item is null))
				return Item.NameOrBareJid;

			return RemoteId;
		}

		/// <summary>
		/// Gets the friendly name from a set of meta-data tags.
		/// </summary>
		/// <param name="MetaData">Meta-data tags.</param>
		/// <returns>Friendly name</returns>
		public static string GetFriendlyName(IEnumerable<Property> MetaData)
		{
			string Apartment = null;
			string Area = null;
			string Building = null;
			string City = null;
			string Class = null;
			string Country = null;
			string Manufacturer = null;
			string MeterLocation = null;
			string MeterNumber = null;
			string Model = null;
			string Name = null;
			string Region = null;
			string Room = null;
			string SerialNumber = null;
			string Street = null;
			string StreetNumber = null;
			string Version = null;

			foreach (Property P in MetaData)
			{
				switch (P.Name.ToUpper())
				{
					case "APT":
						Apartment = P.Value;
						break;

					case "AREA":
						Area = P.Value;
						break;

					case "BLD":
						Building = P.Value;
						break;

					case "CITY":
						City = P.Value;
						break;

					case "COUNTRY":
						Country = P.Value;
						break;

					case "REGION":
						Region = P.Value;
						break;

					case "ROOM":
						Room = P.Value;
						break;

					case "STREET":
						Street = P.Value;
						break;

					case "STREETNR":
						StreetNumber = P.Value;
						break;

					case "CLASS":
						Class = P.Value;
						break;

					case "MAN":
						Manufacturer = P.Value;
						break;

					case "MLOC":
						MeterLocation = P.Value;
						break;

					case "MNR":
						MeterNumber = P.Value;
						break;

					case "MODEL":
						Model = P.Value;
						break;

					case "NAME":
						Name = P.Value;
						break;

					case "SN":
						SerialNumber = P.Value;
						break;

					case "V":
						Version = P.Value;
						break;
				}
			}

			StringBuilder sb = null;

			AppendName(ref sb, Class);
			AppendName(ref sb, Model);
			AppendName(ref sb, Version);
			AppendName(ref sb, Room);
			AppendName(ref sb, Name);
			AppendName(ref sb, Apartment);
			AppendName(ref sb, Building);
			AppendName(ref sb, Street);
			AppendName(ref sb, StreetNumber);
			AppendName(ref sb, Area);
			AppendName(ref sb, City);
			AppendName(ref sb, Region);
			AppendName(ref sb, Country);
			AppendName(ref sb, MeterNumber);
			AppendName(ref sb, MeterLocation);
			AppendName(ref sb, Manufacturer);
			AppendName(ref sb, SerialNumber);

			return sb?.ToString();
		}

		/// <summary>
		/// Gets the friendly name of a legal identity.
		/// </summary>
		/// <param name="Identity">Legal Identity</param>
		/// <returns>Friendly name</returns>
		public static string GetFriendlyName(LegalIdentity Identity)
		{
			string FirstName = null;
			string MiddleName = null;
			string LastName = null;
			string PersonalNumber = null;
			bool HasName = false;

			foreach (Property P in Identity.Properties)
			{
				switch (P.Name.ToUpper())
				{
					case "FIRST":
						FirstName = P.Value;
						HasName = true;
						break;

					case "MIDDLE":
						MiddleName = P.Value;
						HasName = true;
						break;

					case "LAST":
						LastName = P.Value;
						HasName = true;
						break;

					case "PNR":
						PersonalNumber = P.Value;
						break;
				}
			}

			if (HasName)
			{
				StringBuilder sb = null;

				AppendName(ref sb, FirstName);
				AppendName(ref sb, MiddleName);
				AppendName(ref sb, LastName);

				if (!(sb is null))
					return sb.ToString();
			}

			if (!string.IsNullOrEmpty(PersonalNumber))
				return PersonalNumber;

			return Identity.Id;
		}

		private static void AppendName(ref StringBuilder sb, string Value)
		{
			if (!string.IsNullOrEmpty(Value))
			{
				if (sb is null)
					sb = new StringBuilder();
				else
					sb.Append(' ');

				sb.Append(Value);
			}
		}

	}
}
