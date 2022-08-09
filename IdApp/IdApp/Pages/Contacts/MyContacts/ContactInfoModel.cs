using IdApp.Services;
using IdApp.Services.Notification;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Contact Information model, including related notification information.
	/// </summary>
	public class ContactInfoModel
	{
		private readonly ContactInfo contact;
		private readonly NotificationEvent[] events;

		/// <summary>
		/// Contact Information model, including related notification information.
		/// </summary>
		/// <param name="Contact">Contact information.</param>
		/// <param name="Events">Notification events</param>
		public ContactInfoModel(ContactInfo Contact, params NotificationEvent[] Events)
		{
			this.contact = Contact;
			this.events = Events;
		}

		/// <summary>
		/// Contact Information object in database.
		/// </summary>
		public ContactInfo Contact => this.contact;

		/// <summary>
		/// Bare JID of contact.
		/// </summary>
		public CaseInsensitiveString BareJid => this.contact.BareJid;

		/// <summary>
		/// Legal ID of contact.
		/// </summary>
		public CaseInsensitiveString LegalId => this.contact.LegalId;

		/// <summary>
		/// Legal Identity object.
		/// </summary>
		public LegalIdentity LegalIdentity => this.contact.LegalIdentity;

		/// <summary>
		/// Friendly name.
		/// </summary>
		public string FriendlyName => this.contact.FriendlyName;

		/// <summary>
		/// Source ID
		/// </summary>
		public string SourceId => this.contact.SourceId;

		/// <summary>
		/// Partition
		/// </summary>
		public string Partition => this.contact.Partition;

		/// <summary>
		/// Node ID
		/// </summary>
		public string NodeId => this.contact.NodeId;

		/// <summary>
		/// Registry JID
		/// </summary>
		public CaseInsensitiveString RegistryJid => this.contact.RegistryJid;

		/// <summary>
		/// Subscribe to this contact
		/// </summary>
		public bool? SubcribeTo => this.contact.SubcribeTo;

		/// <summary>
		/// Allow subscriptions from this contact
		/// </summary>
		public bool? AllowSubscriptionFrom => this.contact.AllowSubscriptionFrom;

		/// <summary>
		/// The contact is a thing
		/// </summary>
		public bool? IsThing => this.contact.IsThing;

		/// <summary>
		/// If the account is registered as the owner of the thing.
		/// </summary>
		public bool? Owner => this.contact.Owner;

		/// <summary>
		/// Meta-data related to a thing.
		/// </summary>
		public Property[] MetaData => this.contact.MetaData;

		/// <summary>
		/// Notification events.
		/// </summary>
		public NotificationEvent[] Events => this.events;

	}
}
