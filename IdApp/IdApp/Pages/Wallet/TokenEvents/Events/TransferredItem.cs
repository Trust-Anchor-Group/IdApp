using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	public class TransferredItem : OwnershipEventItem
	{
		private readonly Transferred @event;

		/// <summary>
		/// Represents a token creation event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public TransferredItem(Transferred Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Transferred;

		/// <summary>
		/// Seller
		/// </summary>
		public string Seller => this.@event.Seller;
	}
}
