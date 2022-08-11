using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Notification
{
	/// <summary>
	/// Notification service
	/// </summary>
	[Singleton]
	public class NotificationService : LoadableService, INotificationService
	{
		private const int nrButtons = 4;

		private readonly SortedDictionary<CaseInsensitiveString, List<NotificationEvent>>[] events;

		/// <summary>
		/// Notification service
		/// </summary>
		public NotificationService()
		{
			int i;

			this.events = new SortedDictionary<CaseInsensitiveString, List<NotificationEvent>>[nrButtons];

			for (i = 0; i < nrButtons; i++)
				this.events[i] = new SortedDictionary<CaseInsensitiveString, List<NotificationEvent>>();
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="isResuming">Set to <c>true</c> to indicate the app is resuming as opposed to starting cold.</param>
		/// <param name="cancellationToken">Will stop the service load if the token is set.</param>
		public override async Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = null;
			List<NotificationEvent> Events = null;
			string PrevCategory = null;
			int PrevButton = -1;
			int Button;

			IEnumerable<NotificationEvent> LoadedEvents;

			try
			{
				LoadedEvents = await Database.Find<NotificationEvent>("Button", "Category");
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);

				await Database.Clear("Notifications");
				LoadedEvents = new NotificationEvent[0];
			}

			foreach (NotificationEvent Event in LoadedEvents)
			{
				Button = (int)Event.Button;
				if (Button < 0 || Button >= nrButtons)
					continue;

				lock (this.events)
				{
					if (ByCategory is null || Button != PrevButton)
					{
						ByCategory = this.events[Button];
						PrevButton = Button;
					}

					if (Events is null || Event.Category != PrevCategory)
					{
						if (!ByCategory.TryGetValue(Event.Category, out Events))
						{
							Events = new List<NotificationEvent>();
							ByCategory[Event.Category] = Events;
						}

						PrevCategory = Event.Category;
					}

					Events.Add(Event);
				}
			}

			await base.Load(isResuming, cancellationToken);
		}

		/// <summary>
		/// Registers a new event and notifies the user.
		/// </summary>
		/// <param name="Event">Notification event.</param>
		public async Task NewEvent(NotificationEvent Event)
		{
			await Database.Insert(Event);

			int Button = (int)Event.Button;
			if (Button >= 0 && Button < nrButtons)
			{
				lock (this.events)
				{
					SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[Button];

					if (!ByCategory.TryGetValue(Event.Category, out List<NotificationEvent> Events))
					{
						Events = new List<NotificationEvent>();
						ByCategory[Event.Category] = Events;
					}

					Events.Add(Event);
				}

				try
				{
					this.OnNewNotification?.Invoke(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Deletes events for a given button and category.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <param name="Category">Category</param>
		public async Task DeleteEvents(EventButton Button, CaseInsensitiveString Category)
		{
			int ButtonIndex = (int)Button;

			if (ButtonIndex >= 0 && ButtonIndex < nrButtons)
			{
				NotificationEvent[] ToDelete;

				lock (this.events)
				{
					SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[ButtonIndex];

					if (!ByCategory.TryGetValue(Category, out List<NotificationEvent> Events))
						return;

					ToDelete = Events.ToArray();
					ByCategory.Remove(Category);
				}

				await this.DeleteEvents(ToDelete);
			}
		}

		/// <summary>
		/// Deletes a specified set of events.
		/// </summary>
		/// <param name="Events">Events to delete.</param>
		public async Task DeleteEvents(NotificationEvent[] Events)
		{
			try
			{
				await Database.DeleteLazy(Events);
				this.OnNotificationsDeleted?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Gets available notification events for a button.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <returns>Recorded events.</returns>
		public NotificationEvent[] GetEvents(EventButton Button)
		{
			int i = (int)Button;
			if (i < 0 || i >= nrButtons)
				return new NotificationEvent[0];

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				List<NotificationEvent> Result = new();

				foreach (List<NotificationEvent> Events in ByCategory.Values)
					Result.AddRange(Events);

				return Result.ToArray();
			}
		}

		/// <summary>
		/// Gets available categories for a button.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <returns>Recorded categories.</returns>
		public CaseInsensitiveString[] GetCategories(EventButton Button)
		{
			int i = (int)Button;
			if (i < 0 || i >= nrButtons)
				return new CaseInsensitiveString[0];

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				List<CaseInsensitiveString> Result = new();

				foreach (CaseInsensitiveString Category in ByCategory.Keys)
					Result.Add(Category);

				return Result.ToArray();
			}
		}

		/// <summary>
		/// Gets available notification events for a button, sorted by category.
		/// </summary>
		/// <param name="Button">Button</param>
		/// <returns>Recorded events.</returns>
		public SortedDictionary<CaseInsensitiveString, NotificationEvent[]> GetEventsByCategory(EventButton Button)
		{
			int i = (int)Button;
			if (i < 0 || i >= nrButtons)
				return new SortedDictionary<CaseInsensitiveString, NotificationEvent[]>();

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> Result = new();

				foreach (KeyValuePair<CaseInsensitiveString, List<NotificationEvent>> P in ByCategory)
					Result[P.Key] = P.Value.ToArray();

				return Result;
			}
		}

		/// <summary>
		/// Tries to get available notification events.
		/// </summary>
		/// <param name="Button">Event Button</param>
		/// <param name="Category">Notification event category</param>
		/// <param name="Events">Notification events, if found.</param>
		/// <returns>If notification events where found for the given category.</returns>
		public bool TryGetNotificationEvents(EventButton Button, CaseInsensitiveString Category, out NotificationEvent[] Events)
		{
			int i = (int)Button;
			if (i < 0 || i >= nrButtons)
			{
				Events = null;
				return false;
			}

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];

				if (!ByCategory.TryGetValue(Category, out List<NotificationEvent> Events2))
				{
					Events = null;
					return false;
				}

				Events = Events2.ToArray();
				return true;
			}
		}

		/// <summary>
		/// Event raised when a new notification has been logged.
		/// </summary>
		public event EventHandler OnNewNotification;

		/// <summary>
		/// Event raised when notifications have been deleted.
		/// </summary>
		public event EventHandler OnNotificationsDeleted;

		/// <summary>
		/// Number of notifications but button Contacts
		/// </summary>
		public int NrNotificationsContacts => this.Count((int)EventButton.Contacts);

		/// <summary>
		/// Number of notifications but button Things
		/// </summary>
		public int NrNotificationsThings => this.Count((int)EventButton.Things);

		/// <summary>
		/// Number of notifications but button Contracts
		/// </summary>
		public int NrNotificationsContracts => this.Count((int)EventButton.Contracts);

		/// <summary>
		/// Number of notifications but button Wallet
		/// </summary>
		public int NrNotificationsWallet => this.Count((int)EventButton.Wallet);

		private int Count(int Index)
		{
			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> Events = this.events[Index];
				int Result = 0;

				foreach (List<NotificationEvent> List in Events.Values)
					Result += List.Count;

				return Result;
			}
		}

	}
}
