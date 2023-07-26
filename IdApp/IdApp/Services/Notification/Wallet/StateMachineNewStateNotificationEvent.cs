using NeuroFeatures;
using System.Text;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a change in a state-machine associated with a token.
	/// </summary>
	public class StateMachineNewStateNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a change in a state-machine associated with a token.
		/// </summary>
		public StateMachineNewStateNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a change in a state-machine associated with a token.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public StateMachineNewStateNotificationEvent(NewStateEventArgs e)
			: base(e)
		{
			this.NewState = e.NewState;
		}

		/// <summary>
		/// New state of state-machine.
		/// </summary>
		public string NewState { get; set; }

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			StringBuilder sb = new();

			sb.Append(string.Format(LocalizationResourceManager.Current["StateChangedTo"], this.NewState));
			sb.Append(": ");
			sb.Append(await base.GetDescription(ServiceReferences));

			return sb.ToString();
		}
	}
}
