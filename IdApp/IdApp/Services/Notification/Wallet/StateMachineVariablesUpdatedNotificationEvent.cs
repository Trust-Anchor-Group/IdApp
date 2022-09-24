using NeuroFeatures;
using System.Text;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a change in internal variables of a state-machine associated with a token.
	/// </summary>
	public class StateMachineVariablesUpdatedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a change in internal variables of a state-machine associated with a token.
		/// </summary>
		public StateMachineVariablesUpdatedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a change in internal variables of a state-machine associated with a token.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public StateMachineVariablesUpdatedNotificationEvent(VariablesUpdatedEventArgs e)
			: base(e)
		{
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			StringBuilder sb = new();

			sb.Append(LocalizationResourceManager.Current["VariablesUpdated"]);
			sb.Append(": ");
			sb.Append(await base.GetDescription(ServiceReferences));

			return sb.ToString();
		}
	}
}
