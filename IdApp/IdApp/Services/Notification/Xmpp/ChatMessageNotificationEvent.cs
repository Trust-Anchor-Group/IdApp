using IdApp.Pages.Contacts.Chat;
using IdApp.Resx;
using IdApp.Services.Navigation;
using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP;

namespace IdApp.Services.Notification.Xmpp
{
	/// <summary>
	/// Contains information about an incoming chat message.
	/// </summary>
	public class ChatMessageNotificationEvent : XmppNotificationEvent
	{
		/// <summary>
		/// Contains information about an incoming chat message.
		/// </summary>
		public ChatMessageNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about an incoming chat message.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public ChatMessageNotificationEvent(MessageEventArgs e)
			: this(e, e.FromBareJID)
		{
		}

		/// <summary>
		/// Contains information about an incoming chat message.
		/// </summary>
		/// <param name="e">Event arguments</param>
		/// <param name="RemoteBareJid">Remote Bare JID</param>
		public ChatMessageNotificationEvent(MessageEventArgs e, string RemoteBareJid)
			: base()
		{
			this.Category = RemoteBareJid;
			this.BareJid = RemoteBareJid;
			this.Received = DateTime.UtcNow;
			this.Button = EventButton.Contacts;
		}

		/// <summary>
		/// ID of message object being updated
		/// </summary>
		public string ReplaceObjectId { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service References</param>
		/// <returns>Icon</returns>
		public override Task<string> GetCategoryIcon(IServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.User);
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			return ContactInfo.GetFriendlyName(this.BareJid, ServiceReferences);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(IServiceReferences ServiceReferences)
		{
			ContactInfo ContactInfo = await ContactInfo.FindByBareJid(this.BareJid);
			string LegalId = ContactInfo?.LegalId;
			string FriendlyName = await this.GetDescription(ServiceReferences);
			ChatNavigationArgs Args = new(LegalId, this.BareJid, FriendlyName);

			await ServiceReferences.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, this.BareJid);
		}
	}
}
