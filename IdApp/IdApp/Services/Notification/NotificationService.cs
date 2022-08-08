using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Persistence;
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

		private readonly SortedDictionary<string, List<NotificationEvent>>[] events;

		/// <summary>
		/// Notification service
		/// </summary>
		public NotificationService()
		{
			int i;

			this.events = new SortedDictionary<string, List<NotificationEvent>>[nrButtons];

			for (i = 0; i < nrButtons; i++)
				this.events[i] = new SortedDictionary<string, List<NotificationEvent>>();
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="isResuming">Set to <c>true</c> to indicate the app is resuming as opposed to starting cold.</param>
		/// <param name="cancellationToken">Will stop the service load if the token is set.</param>
		public override async Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			SortedDictionary<string, List<NotificationEvent>> ByCategory = null;
			List<NotificationEvent> Events = null;
			string PrevCategory = null;
			int PrevButton = -1;
			int Button;

			foreach (NotificationEvent Event in await Database.Find<NotificationEvent>("Button", "Category"))
			{
				Button = (int)Event.Button;
				if (Button < 0 || Button >= nrButtons)
					continue;

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
				SortedDictionary<string, List<NotificationEvent>> ByCategory = this.events[Button];

				if (!ByCategory.TryGetValue(Event.Category, out List<NotificationEvent> Events))
				{
					Events = new List<NotificationEvent>();
					ByCategory[Event.Category] = Events;
				}

				Events.Add(Event);

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
		/// Event raised when a new notification has been logged.
		/// </summary>
		public event EventHandler OnNewNotification;

		/// <summary>
		/// Number of notifications but button Left1
		/// </summary>
		public int NrNotificationsLeft1 => this.Count(this.events[0]);

		/// <summary>
		/// Number of notifications but button Left2
		/// </summary>
		public int NrNotificationsLeft2 => this.Count(this.events[1]);

		/// <summary>
		/// Number of notifications but button Right1
		/// </summary>
		public int NrNotificationsRight1 => this.Count(this.events[2]);

		/// <summary>
		/// Number of notifications but button Right2
		/// </summary>
		public int NrNotificationsRight2 => this.Count(this.events[3]);

		private int Count(SortedDictionary<string, List<NotificationEvent>> Events)
		{
			int Result = 0;

			foreach (List<NotificationEvent> List in Events.Values)
				Result += List.Count;

			return Result;
		}
		
	}
}
