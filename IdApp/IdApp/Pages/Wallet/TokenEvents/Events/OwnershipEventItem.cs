using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token ownership event.
	/// </summary>
	public abstract class OwnershipEventItem : ValueEventItem
	{
		private readonly TokenOwnershipEvent @event;

		/// <summary>
		/// Represents a token ownership event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public OwnershipEventItem(TokenOwnershipEvent Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Owner
		/// </summary>
		public string Owner => this.@event.Owner;

		/// <summary>
		/// Ownership contract
		/// </summary>
		public string OwnershipContract => this.@event.OwnershipContract;
	}
}
