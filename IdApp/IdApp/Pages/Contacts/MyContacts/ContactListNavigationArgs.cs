using IdApp.Services;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Actions to take when a contact has been selected.
	/// </summary>
	public enum SelectContactAction
	{
		/// <summary>
		/// Make a payment to contact.
		/// </summary>
		MakePayment,

		/// <summary>
		/// View the identity.
		/// </summary>
		ViewIdentity,

		/// <summary>
		/// Embed link to ID in chat
		/// </summary>
		Select
	}

	/// <summary>
	/// Holds navigation parameters specific to views displaying a list of contacts.
	/// </summary>
	public class ContactListNavigationArgs : NavigationArgs
	{
		private readonly string description;
		private readonly SelectContactAction action;
		private readonly TaskCompletionSource<ContactInfoModel> selection;
		private readonly SortedDictionary<CaseInsensitiveString, NotificationEvent[]> notificationEvents;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		public ContactListNavigationArgs()
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		/// <param name="Description">Description presented to user.</param>
		/// <param name="Action">Action to take when a contact has been selected.</param>
		public ContactListNavigationArgs(string Description, SelectContactAction Action)
			: this(Description, Action, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		/// <param name="Description">Description presented to user.</param>
		/// <param name="Action">Action to take when a contact has been selected.</param>
		/// <param name="NotificationEvents">Pending notification events.</param>
		public ContactListNavigationArgs(string Description, SelectContactAction Action, SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents)
		{
			this.description = Description;
			this.action = Action;
			this.notificationEvents = NotificationEvents;
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		/// <param name="Description">Description presented to user.</param>
		/// <param name="Selection">Selection source, where selected item will be stored, or null if cancelled.</param>
		public ContactListNavigationArgs(string Description, TaskCompletionSource<ContactInfoModel> Selection)
			: this(Description, SelectContactAction.Select)
		{
			this.selection = Selection;
		}

		/// <summary>
		/// Description presented to user.
		/// </summary>
		public string Description => this.description;

		/// <summary>
		/// Action to take when a contact has been selected.
		/// </summary>
		public SelectContactAction Action => this.action;

		/// <summary>
		/// Selection source, if selecting identity.
		/// </summary>
		public TaskCompletionSource<ContactInfoModel> Selection => this.selection;

		/// <summary>
		/// If the user should be able to scane QR Codes.
		/// </summary>
		public bool CanScanQrCode { get; set; }

		/// <summary>
		/// If notification events are available.
		/// </summary>
		public bool HasNotificationEvents => (this.notificationEvents?.Count ?? 0) > 0;

		/// <summary>
		/// Tries to get available notification events.
		/// </summary>
		/// <param name="Category">Notification event category</param>
		/// <param name="Events">Notification events, if found.</param>
		/// <returns>If notification events where found for the given category.</returns>
		public bool TryGetNotificationEvents(CaseInsensitiveString Category, out NotificationEvent[] Events)
		{
			if (this.notificationEvents is null)
			{
				Events = null;
				return false;
			}
			else
				return this.notificationEvents.TryGetValue(Category, out Events);
		}

		/// <summary>
		/// Available notification categories.
		/// </summary>
		public CaseInsensitiveString[] NotificationCategories
		{
			get
			{
				if (this.notificationEvents is null)
					return new CaseInsensitiveString[0];
				else
				{
					CaseInsensitiveString[] Result = new CaseInsensitiveString[this.notificationEvents.Count];
					this.notificationEvents.Keys.CopyTo(Result, 0);
					return Result;
				}
			}
		}

	}
}
