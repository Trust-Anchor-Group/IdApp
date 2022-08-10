using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Notification
{
	/// <summary>
	/// Interface for push notification services.
	/// </summary>
	[DefaultImplementation(typeof(NotificationService))]
	public interface INotificationService : ILoadableService
	{
		/// <summary>
		/// Registers a new event and notifies the user.
		/// </summary>
		/// <param name="Event">Notification event.</param>
		Task NewEvent(NotificationEvent Event);

		/// <summary>
		/// Deletes events for a given button and category.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <param name="Category">Category</param>
		Task DeleteEvents(EventButton Button, CaseInsensitiveString Category);

		/// <summary>
		/// Gets available categories for a button.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <returns>Recorded categories.</returns>
		CaseInsensitiveString[] GetCategories(EventButton Button);

		/// <summary>
		/// Gets available notification events for a button.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <returns>Recorded events.</returns>
		NotificationEvent[] GetEvents(EventButton Button);

		/// <summary>
		/// Gets available notification events for a button, sorted by category.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <returns>Recorded events.</returns>
		SortedDictionary<CaseInsensitiveString, NotificationEvent[]> GetEventsByCategory(EventButton Button);

		/// <summary>
		/// Tries to get available notification events.
		/// </summary>
		/// <param name="Button">Event Button</param>
		/// <param name="Category">Notification event category</param>
		/// <param name="Events">Notification events, if found.</param>
		/// <returns>If notification events where found for the given category.</returns>
		bool TryGetNotificationEvents(EventButton Button, CaseInsensitiveString Category, out NotificationEvent[] Events);

		/// <summary>
		/// Event raised when a new notification has been logged.
		/// </summary>
		event EventHandler OnNewNotification;

		/// <summary>
		/// Event raised when notifications have been deleted.
		/// </summary>
		event EventHandler OnNotificationsDeleted;

		/// <summary>
		/// Number of notifications but button Left1
		/// </summary>
		int NrNotificationsLeft1 { get; }

		/// <summary>
		/// Number of notifications but button Left2
		/// </summary>
		int NrNotificationsLeft2 { get; }

		/// <summary>
		/// Number of notifications but button Right1
		/// </summary>
		int NrNotificationsRight1 { get; }

		/// <summary>
		/// Number of notifications but button Right2
		/// </summary>
		int NrNotificationsRight2 { get; }
	}
}
