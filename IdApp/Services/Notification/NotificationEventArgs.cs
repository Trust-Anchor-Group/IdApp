using System;

namespace IdApp.Services.Notification
{
	/// <summary>
	/// Delegate for notification event handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate void NotificationEventHandler(object Sender, NotificationEventArgs e);

	/// <summary>
	/// Event argument for notification events.
	/// </summary>
	public class NotificationEventArgs : EventArgs
	{
		/// <summary>
		/// Event argument for notification events.
		/// </summary>
		/// <param name="Event">Referenced event.</param>
		public NotificationEventArgs(NotificationEvent Event)
			: base()
		{
			this.Event = Event;
		}

		/// <summary>
		/// Referenced event.
		/// </summary>
		public NotificationEvent Event { get; }
	}
}
