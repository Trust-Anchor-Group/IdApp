using IdApp.Services;
using IdApp.Services.Notification;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.Forms;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Contact Information model, including related notification information.
	/// </summary>
	public class ContactInfoModel
	{
		private readonly ServiceReferences references;
		private readonly ContactInfo contact;
		private readonly NotificationEvent[] events;

		/// <summary>
		/// Contact Information model, including related notification information.
		/// </summary>
		/// <param name="References">Service references.</param>
		/// <param name="Contact">Contact information.</param>
		/// <param name="Events">Notification events</param>
		public ContactInfoModel(ServiceReferences References, ContactInfo Contact, params NotificationEvent[] Events)
		{
			this.references = References;
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

		/// <summary>
		/// If the contact has associated events.
		/// </summary>
		public bool HasEvents => this.events is not null && this.events.Length > 0;

		/// <summary>
		/// Number of events associated with contact.
		/// </summary>
		public int NrEvents => this.events?.Length ?? 0;

		/// <summary>
		/// A color representing the current connection state of the contact.
		/// </summary>
		public Color ConnectionColor
		{
			get
			{
				if (string.IsNullOrEmpty(this.contact.BareJid))
					return Color.Transparent;

				RosterItem Item = this.references.XmppService.Xmpp[this.contact.BareJid];
				if (Item is null)
					return Color.Transparent;

				if (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both)
					return Color.Transparent;

				if (!Item.HasLastPresence)
					return Color.LightSalmon;

				return Item.LastPresence.Availability switch
				{
					Availability.Online or Availability.Chat => Color.LightGreen,
					Availability.Away or Availability.ExtendedAway => Color.LightYellow,
					_ => Color.LightSalmon,
				};
			}
		}

	}
}
