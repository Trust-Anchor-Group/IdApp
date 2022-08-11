using NeuroFeatures;

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
	}
}
