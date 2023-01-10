using System;

namespace IdApp.Services.Notification
{
	/// <summary>
	/// Delegate for notification events handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate void NotificationEventsHandler(object Sender, NotificationEventsArgs e);

	/// <summary>
	/// Event argument for notification events.
	/// </summary>
	public class NotificationEventsArgs : EventArgs
	{
		/// <summary>
		/// Event argument for notification events.
		/// </summary>
		/// <param name="Events">Referenced event.</param>
		public NotificationEventsArgs(NotificationEvent[] Events)
			: base()
		{
			this.Events = Events;
		}

		/// <summary>
		/// Referenced events.
		/// </summary>
		public NotificationEvent[] Events { get; }
	}
}
