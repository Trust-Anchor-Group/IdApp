using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	public class CreatedItem : OwnershipEventItem
	{
		private readonly Created @event;

		/// <summary>
		/// Represents a token creation event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public CreatedItem(Created Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Created;

		/// <summary>
		/// Creator
		/// </summary>
		public string Creator => this.@event.Creator;
	}
}
