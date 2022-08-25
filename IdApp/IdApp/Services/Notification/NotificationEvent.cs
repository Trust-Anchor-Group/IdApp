using System;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace IdApp.Services.Notification
{
	/// <summary>
	/// Button on which event is to be displayed.
	/// </summary>
	public enum EventButton
	{
		/// <summary>
		/// Contacts (Left 1)
		/// </summary>
		Contacts = 0,

		/// <summary>
		/// Things (Left 2)
		/// </summary>
		Things = 1,

		/// <summary>
		/// Contracts (Right 1)
		/// </summary>
		Contracts = 2,

		/// <summary>
		/// Wallet (Right 2)
		/// </summary>
		Wallet = 3
	}

	/// <summary>
	/// Abstract base class of notification events.
	/// </summary>
	[CollectionName("Notifications")]
	[TypeName(TypeNameSerialization.FullName)]
	[Index("Button", "Category")]
	public abstract class NotificationEvent
	{
		/// <summary>
		/// Abstract base class of notification events.
		/// </summary>
		public NotificationEvent()
		{
		}

		/// <summary>
		/// Object ID of notification event.
		/// </summary>
		[ObjectId]
		public string ObjectId { get; set; }

		/// <summary>
		/// When event was received
		/// </summary>
		public DateTime Received { get; set; }

		/// <summary>
		/// Category string. Events having the same category are grouped and processed together.
		/// </summary>
		public CaseInsensitiveString Category { get; set; }

		/// <summary>
		/// Button on which notification should be displayed
		/// </summary>
		public EventButton Button { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences"></param>
		/// <returns></returns>
		public abstract Task<string> GetCategoryIcon(ServiceReferences ServiceReferences);

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public abstract Task<string> GetDescription(ServiceReferences ServiceReferences);

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public abstract Task Open(ServiceReferences ServiceReferences);

		/// <summary>
		/// Performs perparatory tasks, that will simplify opening the notification.
		/// </summary>
		/// <param name="ServiceReferences">Service references.</param>
		public virtual Task Prepare(ServiceReferences ServiceReferences)
		{
			return Task.CompletedTask;
		}
	}
}
