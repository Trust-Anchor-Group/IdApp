using NeuroFeatures;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a token that has been added.
	/// </summary>
	public class TokenAddedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a token that has been added.
		/// </summary>
		public TokenAddedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a token that has been added.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenAddedNotificationEvent(TokenEventArgs e)
			: base(e)
		{
		}
	}
}
