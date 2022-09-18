using IdApp.Pages.Things.CanRead;
using IdApp.Resx;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Provisioning;
using Waher.Things.SensorData;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Things
{
	/// <summary>
	/// Contains information about a request to read a thing.
	/// </summary>
	public class CanReadNotificationEvent : ThingNotificationEvent
	{
		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		public CanReadNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public CanReadNotificationEvent(CanReadEventArgs e)
			: base(e)
		{
			this.Fields = e.Fields;
			this.FieldTypes = e.FieldTypes;
		}

		/// <summary>
		/// Fields requested
		/// </summary>
		public string[] Fields { get; set; }

		/// <summary>
		/// Field types requested.
		/// </summary>
		public FieldType FieldTypes { get; set; }

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

			return string.Format(LocalizationResourceManager.Current["ReadoutRequestText"], RemoteName, ThingName);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			string ThingName = await ContactInfo.GetFriendlyName(this.BareJid, ServiceReferences);
			string RemoteName = await ContactInfo.GetFriendlyName(this.RemoteJid, ServiceReferences);

			await ServiceReferences.NavigationService.GoToAsync(nameof(CanReadPage), new CanReadNavigationArgs(this, ThingName, RemoteName));
		}
	}
}
