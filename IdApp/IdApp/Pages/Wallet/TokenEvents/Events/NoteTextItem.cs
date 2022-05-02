using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	public class NoteTextItem : NoteItem
	{
		/// <summary>
		/// Represents a token creation event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public NoteTextItem(NoteText Event)
			: base(Event)
		{
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.NoteText;
	}
}
