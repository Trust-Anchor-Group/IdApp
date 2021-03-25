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
	[Index("BareJid")]
	[Index("LegalId")]
	public class ContactInfo
	{
		private string objectId = null;
		private CaseInsensitiveString bareJid = CaseInsensitiveString.Empty;
		private CaseInsensitiveString legalId = CaseInsensitiveString.Empty;
		private LegalIdentity legalIdentity = null;
		private string friendlyName = string.Empty;
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
