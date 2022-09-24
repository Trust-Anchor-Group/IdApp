using IdApp.Services;
using IdApp.Services.Notification;
using System;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a notification event that is not associate with a referenced contract.
	/// </summary>
	public class EventModel : ObservableObject, IItemGroup
	{
		private readonly IServiceReferences references;

		/// <summary>
		/// Creates an instance of the <see cref="EventModel"/> class.
		/// </summary>
		/// <param name="Received">When event was received.</param>
		/// <param name="Icon">Icon of event.</param>
		/// <param name="Description">Description of event.</param>
		/// <param name="Event">Notification event object.</param>
		/// <param name="References">Service references.</param>
		public EventModel(DateTime Received, string Icon, string Description, NotificationEvent Event, IServiceReferences References)
		{
			this.Received = Received;
			this.Icon = Icon;
			this.Description = Description;
			this.Event = Event;
			this.references = References;

			this.ClickedCommand = new Command(_ => this.Clicked());
		}

		/// <summary>
		/// When event was received.
		/// </summary>
		public DateTime Received { get; }

		/// <summary>
		/// Icon of event.
		/// </summary>
		public string Icon { get; }

		/// <summary>
		/// Description of event.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Notification event object.
		/// </summary>
		public NotificationEvent Event { get; }

		/// <summary>
		/// Unique name used to compare items.
		/// </summary>
		public string UniqueName => this.Event.ObjectId;

		/// <summary>
		/// Command executed when the token has been clicked or tapped.
		/// </summary>
		public ICommand ClickedCommand { get; }

		/// <summary>
		/// Opens the notification event.
		/// </summary>
		public void Clicked()
		{
			this.references.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await this.Event.Open(this.references);

					if (this.Event.DeleteWhenOpened)
						await this.references.NotificationService.DeleteEvents(this.Event);
				}
				catch (Exception ex)
				{
					this.references.LogService.LogException(ex);
				}
			});
		}
	}
}
