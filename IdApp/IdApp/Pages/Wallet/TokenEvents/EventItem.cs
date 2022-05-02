using IdApp.Pages.Wallet.TokenEvents.Events;
using IdApp.Resx;
using NeuroFeatures.Events;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// Types of events
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// Creation of token
		/// </summary>
		Created,

		/// <summary>
		/// Destruction of token
		/// </summary>
		Destroyed,

		/// <summary>
		/// Token transferred to new owner
		/// </summary>
		Transferred,

		/// <summary>
		/// Text note made by owner at the time.
		/// </summary>
		NoteText,

		/// <summary>
		/// XML note made by owner at the time.
		/// </summary>
		NoteXml
	}

	/// <summary>
	/// Represents a token event.
	/// </summary>
	public abstract class EventItem
	{
		private readonly TokenEvent @event;

		/// <summary>
		/// Represents a token event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public EventItem(TokenEvent Event)
		{
			this.@event = Event;

			this.ViewIdCommand = new Command(async P => await this.ViewId((string)P));
			this.ViewContractCommand = new Command(async P => await this.ViewContract((string)P));
		}

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenId => this.@event.TokenId;

		/// <summary>
		/// Personal
		/// </summary>
		public bool Personal => this.@event.Personal;

		/// <summary>
		/// Timestamp
		/// </summary>
		public DateTime Timestamp => this.@event.Timestamp;

		/// <summary>
		/// When event expires
		/// </summary>
		public DateTime Expires => this.@event.Expires;

		/// <summary>
		/// Required Archiving time (after event expires)
		/// </summary>
		public Duration? ArchiveRequired => this.@event.ArchiveRequired;

		/// <summary>
		/// Optional Archiving time (after required archiving time elapses).
		/// </summary>
		public Duration? ArchiveOptional => this.@event.ArchiveOptional;

		/// <summary>
		/// Type of event.
		/// </summary>
		public abstract EventType Type { get; }

		/// <summary>
		/// Command executed when the user wants to view an ID
		/// </summary>
		public ICommand ViewIdCommand { get; }

		/// <summary>
		/// Command executed when the user wants to view a smart contract
		/// </summary>
		public ICommand ViewContractCommand { get; }

		/// <summary>
		/// Creates an Event Item view model for a token event.
		/// </summary>
		/// <param name="Event">Token event.</param>
		/// <returns>View model of token event.</returns>
		public static EventItem Create(TokenEvent Event)
		{
			if (Event is Created Created)
				return new CreatedItem(Created);
			else if (Event is Destroyed Destroyed)
				return new DestroyedItem(Destroyed);
			else if (Event is NoteText NoteText)
				return new NoteTextItem(NoteText);
			else if (Event is NoteXml NoteXml)
				return new NoteXmlItem(NoteXml);
			else if (Event is Transferred Transferred)
				return new TransferredItem(Transferred);
			else
			{
				return new NoteTextItem(new NoteText()
				{
					ArchiveOptional = Event.ArchiveOptional,
					ArchiveRequired = Event.ArchiveRequired,
					Expires = Event.Expires,
					Personal = Event.Personal,
					Timestamp = Event.Timestamp,
					TokenId = Event.TokenId,
					Note = AppResources.UnrecognizedEventType + " " + Event.GetType().FullName
				});
			}
		}

		/// <summary>
		/// Shows a Legal Identity to the suer.
		/// </summary>
		/// <param name="IdentityId">Identity ID</param>
		public async Task ViewId(string IdentityId)
		{
			// TODO
		}

		/// <summary>
		/// Shows a Smart Contract to the user.
		/// </summary>
		/// <param name="ContractId">Contract ID</param>
		public async Task ViewContract(string ContractId)
		{
			// TODO
		}

	}
}
