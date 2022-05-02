using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	public class DestroyedItem : OwnershipEventItem
	{
		private readonly Destroyed @event;

		/// <summary>
		/// Represents a token creation event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public DestroyedItem(Destroyed Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Destroyed;
	}
}
