using IdApp.Pages.Contacts.Chat;
using IdApp.Resx;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Provisioning;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Things
{
	/// <summary>
	/// Contains information about a request to read a thing.
	/// </summary>
	public class CanControlNotificationEvent : ThingNotificationEvent
	{
		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		public CanControlNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public CanControlNotificationEvent(CanControlEventArgs e)
			: base(e)
		{
			this.Parameters = e.Parameters;
		}

		/// <summary>
		/// Fields requested
		/// </summary>
		public string[] Parameters { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service References</param>
		/// <returns>Icon</returns>
		public override Task<string> GetCategoryIcon(ServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.Things);
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(ServiceReferences ServiceReferences)
		{
			string ThingName = await ContactInfo.GetFriendlyName(this.BareJid, this.SourceId, this.PartitionId, this.NodeId, ServiceReferences);
			string RemoteName = await ContactInfo.GetFriendlyName(this.RemoteJid, ServiceReferences);

			return string.Format(LocalizationResourceManager.Current["ControlRequestText"], RemoteName, ThingName);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			ContactInfo ContactInfo = await ContactInfo.FindByBareJid(this.BareJid);
			string LegalId = ContactInfo?.LegalId;
			string FriendlyName = await this.GetDescription(ServiceReferences);

			await ServiceReferences.NavigationService.GoToAsync(nameof(ChatPage),
				new ChatNavigationArgs(LegalId, this.BareJid, FriendlyName)
				{
					UniqueId = BareJid
				});
		}
	}
}
