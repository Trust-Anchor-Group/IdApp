using Waher.Networking.XMPP.Provisioning;

namespace IdApp.Services.Notification.Things
{
	/// <summary>
	/// Abstract base class of thing notification events.
	/// </summary>
	public abstract class ThingNotificationEvent : ProvisioningNotificationEvent
	{
		/// <summary>
		/// Abstract base class of thing notification events.
		/// </summary>
		public ThingNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class of thing notification events.
		/// </summary>
		public ThingNotificationEvent(NodeQuestionEventArgs e)
			: base(e)
		{
			this.NodeId = e.NodeId;
			this.PartitionId = e.Partition;
			this.SourceId = e.SourceId;
		}

		/// <summary>
		/// Node ID of thing.
		/// </summary>
		public string NodeId { get; set; }

		/// <summary>
		/// Source ID of thing.
		/// </summary>
		public string SourceId { get; set; }

		/// <summary>
		/// Partition ID of thing.
		/// </summary>
		public string PartitionId { get; set; }
	}
}
