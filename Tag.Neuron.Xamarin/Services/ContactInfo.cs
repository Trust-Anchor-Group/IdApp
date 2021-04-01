using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Persistence.Filters;

namespace Tag.Neuron.Xamarin.Services
{
	/// <summary>
	/// Contains information about a contact.
	/// </summary>
	[CollectionName("ContactInformation")]
	[TypeName(TypeNameSerialization.None)]
	[Index("BareJid", "SourceId", "Partition", "NodeId")]
	[Index("LegalId")]
	public class ContactInfo
	{
		private string objectId = null;
		private CaseInsensitiveString bareJid = CaseInsensitiveString.Empty;
		private CaseInsensitiveString legalId = CaseInsensitiveString.Empty;
		private LegalIdentity legalIdentity = null;
		private string friendlyName = string.Empty;
		private string sourceId = string.Empty;
		private string partition = string.Empty;
		private string nodeId = string.Empty;
		private string registryJid = string.Empty;
		private bool? subcribeTo = null;
		private bool? allowSubscriptionFrom = null;
		private bool? isThing = null;

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
	}
}
