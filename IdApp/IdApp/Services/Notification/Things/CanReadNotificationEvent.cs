using IdApp.Pages.Things.CanRead;
using IdApp.Resx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Things;
using Waher.Things.SensorData;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Things
{
	/// <summary>
	/// Contains information about a request to read a thing.
	/// </summary>
	public class CanReadNotificationEvent : ThingNotificationEvent
	{
		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		public CanReadNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public CanReadNotificationEvent(CanReadEventArgs e)
			: base(e)
		{
			this.Fields = e.Fields;
			this.FieldTypes = e.FieldTypes;
		}

		/// <summary>
		/// Fields requested
		/// </summary>
		public string[] Fields { get; set; }

		/// <summary>
		/// All Fields available
		/// </summary>
		public string[] AllFields { get; set; }

		/// <summary>
		/// Field types requested.
		/// </summary>
		public FieldType FieldTypes { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service References</param>
		/// <returns>Icon</returns>
		public override Task<string> GetCategoryIcon(ServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.Things);
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(ServiceReferences ServiceReferences)
		{
			string ThingName = await ContactInfo.GetFriendlyName(this.BareJid, this.SourceId, this.PartitionId, this.NodeId, ServiceReferences);
			string RemoteName = await ContactInfo.GetFriendlyName(this.RemoteJid, ServiceReferences);

			return string.Format(LocalizationResourceManager.Current["ReadoutRequestText"], RemoteName, ThingName);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			string ThingName = await ContactInfo.GetFriendlyName(this.BareJid, ServiceReferences);
			string RemoteName = await ContactInfo.GetFriendlyName(this.RemoteJid, ServiceReferences);

			await ServiceReferences.NavigationService.GoToAsync(nameof(CanReadPage), new CanReadNavigationArgs(this, ThingName, RemoteName));
		}

		/// <summary>
		/// Performs perparatory tasks, that will simplify opening the notification.
		/// </summary>
		/// <param name="ServiceReferences">Service references.</param>
		public override async Task Prepare(ServiceReferences ServiceReferences)
		{
			await base.Prepare(ServiceReferences);

			string[] Fields = await GetAvailableFieldNames(this.BareJid, new ThingReference(this.NodeId, this.SourceId, this.PartitionId), ServiceReferences);

			if (Fields is not null)
			{
				this.AllFields = Fields;
				await Database.Update(this);
			}
		}

		/// <summary>
		/// Gets available fields for a thing.
		/// </summary>
		/// <param name="BareJid">Bare JID</param>
		/// <param name="Thing">Thing reference</param>
		/// <param name="ServiceReferences">Service references</param>
		/// <returns>Array of available field names, or null if not able.</returns>
		public static async Task<string[]> GetAvailableFieldNames(string BareJid, ThingReference Thing, ServiceReferences ServiceReferences)
		{
			try
			{
				RosterItem Item = ServiceReferences.XmppService.Xmpp?.GetRosterItem(BareJid);
				if (Item is null || !Item.HasLastPresence || !Item.LastPresence.IsOnline)
					return null;

				SortedDictionary<string, bool> Fields = new();
				SensorDataClientRequest Request = ServiceReferences.XmppService.IoT.SensorClient.RequestReadout(Item.LastPresenceFullJid,
					new ThingReference[] { Thing }, FieldType.All);
				TaskCompletionSource<bool> Done = new();

				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					foreach (Field Field in NewFields)
						Fields[Field.Name] = true;

					return Task.CompletedTask;
				};

				Request.OnStateChanged += (Sender, NewState) =>
				{
					switch (NewState)
					{
						case SensorDataReadoutState.Done:
							Done.TrySetResult(true);
							break;

						case SensorDataReadoutState.Cancelled:
						case SensorDataReadoutState.Failure:
							Done.TrySetResult(false);
							break;
					}

					return Task.CompletedTask;
				};

				Task _ = Task.Delay(30000).ContinueWith((_) => Done.TrySetResult(false));

				if (await Done.Task)
				{
					string[] Fields2 = new string[Fields.Count];
					Fields.Keys.CopyTo(Fields2, 0);

					return Fields2;
				}
				else
					return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

	}
}
