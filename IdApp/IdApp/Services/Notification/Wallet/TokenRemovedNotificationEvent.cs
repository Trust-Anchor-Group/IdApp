using NeuroFeatures;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a token that has been removed.
	/// </summary>
	public class TokenRemovedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a token that has been removed.
		/// </summary>
		public TokenRemovedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a token that has been removed.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenRemovedNotificationEvent(TokenEventArgs e)
			: base(e)
		{
		}
	}
}
