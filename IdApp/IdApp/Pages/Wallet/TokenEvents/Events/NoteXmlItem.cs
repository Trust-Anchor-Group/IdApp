using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	public class NoteXmlItem : NoteItem
	{
		/// <summary>
		/// Represents a token creation event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public NoteXmlItem(NoteXml Event)
			: base(Event)
		{
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.NoteXml;
	}
}
