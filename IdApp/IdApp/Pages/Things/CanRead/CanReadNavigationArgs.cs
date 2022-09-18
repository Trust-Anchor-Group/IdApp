using IdApp.Services.Notification.Things;
using Waher.Things.SensorData;

namespace IdApp.Pages.Things.CanRead
{
	/// <summary>
	/// Holds navigation parameters specific to displaying the is-friend provisuioning question.
	/// </summary>
	public class CanReadNavigationArgs : ThingNavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="CanReadNavigationArgs"/> class.
		/// </summary>
		public CanReadNavigationArgs() { }

		/// <summary>
		/// Creates a new instance of the <see cref="CanReadNavigationArgs"/> class.
		/// </summary>
		/// <param name="Event">Notification event object.</param>
		/// <param name="FriendlyName">Friendly name of device.</param>
		/// <param name="RemoteFriendlyName">Friendly name of remote entity.</param>
		public CanReadNavigationArgs(CanReadNotificationEvent Event, string FriendlyName, string RemoteFriendlyName)
			: base(Event, FriendlyName, RemoteFriendlyName)
		{
			this.Fields = Event.Fields;
			this.FieldTypes = Event.FieldTypes;
		}

		/// <summary>
		/// Fields
		/// </summary>
		public string[] Fields { get; }

		/// <summary>
		/// Field Types
		/// </summary>
		public FieldType FieldTypes { get; }
	}
}
