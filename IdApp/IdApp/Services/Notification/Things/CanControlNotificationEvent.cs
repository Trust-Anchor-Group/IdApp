using IdApp.Pages.Things.CanControl;
using IdApp.Resx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Things;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Things
{
	/// <summary>
	/// Contains information about a request to read a thing.
	/// </summary>
	public class CanControlNotificationEvent : ThingNotificationEvent
	{
		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		public CanControlNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a request to read a thing.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public CanControlNotificationEvent(CanControlEventArgs e)
			: base(e)
		{
			this.Parameters = e.Parameters;
		}

		/// <summary>
		/// Parameters requested
		/// </summary>
		public string[] Parameters { get; set; }

		/// <summary>
		/// All parameters available
		/// </summary>
		public string[] AllParameters { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service References</param>
		/// <returns>Icon</returns>
		public override Task<string> GetCategoryIcon(IServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.Things);
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			string ThingName = await ContactInfo.GetFriendlyName(this.BareJid, this.SourceId, this.PartitionId, this.NodeId, ServiceReferences);
			string RemoteName = await ContactInfo.GetFriendlyName(this.RemoteJid, ServiceReferences);

			return string.Format(LocalizationResourceManager.Current["ControlRequestText"], RemoteName, ThingName);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(IServiceReferences ServiceReferences)
		{
			string ThingName = await ContactInfo.GetFriendlyName(this.BareJid, ServiceReferences);
			string RemoteName = await ContactInfo.GetFriendlyName(this.RemoteJid, ServiceReferences);

			await ServiceReferences.NavigationService.GoToAsync(nameof(CanControlPage), new CanControlNavigationArgs(this, ThingName, RemoteName));
		}

		/// <summary>
		/// Performs perparatory tasks, that will simplify opening the notification.
		/// </summary>
		/// <param name="ServiceReferences">Service references.</param>
		public override async Task Prepare(IServiceReferences ServiceReferences)
		{
			await base.Prepare(ServiceReferences);

			string[] Parameters = await GetAvailableParameterNames(this.BareJid, new ThingReference(this.NodeId, this.SourceId, this.PartitionId), ServiceReferences);

			if (Parameters is not null)
			{
				this.AllParameters = Parameters;
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
		public static async Task<string[]> GetAvailableParameterNames(string BareJid, ThingReference Thing, IServiceReferences ServiceReferences)
		{
			try
			{
				RosterItem Item = ServiceReferences.XmppService.GetRosterItem(BareJid);
				if (Item is null || !Item.HasLastPresence || !Item.LastPresence.IsOnline)
					return null;

				TaskCompletionSource<DataForm> FormTask = new();

				ServiceReferences.XmppService.GetControlForm(Item.LastPresenceFullJid,
					System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
					(sender, e) =>
					{
						if (e.Ok)
							FormTask.TrySetResult(e.Form);
						else
							FormTask.TrySetResult(null);

						return Task.CompletedTask;
					}, null, new ThingReference[] { Thing });

				Task _ = Task.Delay(30000).ContinueWith((_) => FormTask.TrySetResult(null));

				DataForm Form = await FormTask.Task;

				if (Form is not null)
				{
					SortedDictionary<string, bool> Parameters = new();

					foreach (Field Field in Form.Fields)
					{
						if (!Field.ReadOnly && !string.IsNullOrEmpty(Field.Var))
							Parameters[Field.Var] = true;
					}

					string[] Parameters2 = new string[Parameters.Count];
					Parameters.Keys.CopyTo(Parameters2, 0);

					return Parameters2;
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
