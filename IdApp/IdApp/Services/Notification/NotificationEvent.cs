﻿using System;
using System.Threading.Tasks;
using Waher.Persistence.Attributes;

namespace IdApp.Services.Notification
{
	/// <summary>
	/// Button on which event is to be displayed.
	/// </summary>
	public enum EventButton
	{
		/// <summary>
		/// Left 1
		/// </summary>
		Left1 = 0,

		/// <summary>
		/// Left 2
		/// </summary>
		Left2 = 1,

		/// <summary>
		/// Right 1
		/// </summary>
		Right1 = 2,

		/// <summary>
		/// Right 2
		/// </summary>
		Right2 = 3
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
		public string Category { get; set; }

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
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public abstract Task<string> GetCategoryDescription(ServiceReferences ServiceReferences);

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public abstract Task Open(ServiceReferences ServiceReferences);
	}
}
