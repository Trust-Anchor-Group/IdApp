using IdApp.Services.Notification;
using System;
using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a notification event that is not associate with a referenced contract.
	/// </summary>
	public class EventModel : ObservableObject, IItemGroup
	{
		/// <summary>
		/// Creates an instance of the <see cref="EventModel"/> class.
		/// </summary>
		/// <param name="Received">When event was received.</param>
		/// <param name="Icon">Icon of event.</param>
		/// <param name="Description">Description of event.</param>
		/// <param name="Event">Notification event object.</param>
		public EventModel(DateTime Received, string Icon, string Description, NotificationEvent Event)
		{
			this.Received = Received;
			this.Icon = Icon;
			this.Description = Description;
			this.Event = Event;
		}

		/// <summary>
		/// When event was received.
		/// </summary>
		public DateTime Received { get; }

		/// <summary>
		/// Icon of event.
		/// </summary>
		public string Icon { get; }

		/// <summary>
		/// Description of event.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Notification event object.
		/// </summary>
		public NotificationEvent Event { get; }

		/// <summary>
		/// Unique name used to compare items.
		/// </summary>
		public string UniqueName => this.Event.ObjectId;
	}
}
