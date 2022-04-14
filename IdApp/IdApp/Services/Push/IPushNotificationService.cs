using System.Threading.Tasks;
using Waher.Networking.XMPP.Push;

namespace IdApp.Services.Push
{
	/// <summary>
	/// Push messaging service used.
	/// </summary>
	public enum PushMessagingService
	{
		/// <summary>
		/// firebase.google.com
		/// </summary>
		Firebase
	}

	/// <summary>
	/// Interface for push notification services.
	/// </summary>
	public interface IPushNotificationService
	{
		/// <summary>
		/// New token received from push notification back-end.
		/// </summary>
		/// <param name="TokenInformation">Token information.</param>
		Task NewToken(TokenInformation TokenInformation);

		/// <summary>
		/// Tries to get a token from a push notification service.
		/// </summary>
		/// <param name="Source">Source of token</param>
		/// <param name="Token">Token, if found.</param>
		/// <returns>If a token was found for the corresponding source.</returns>
		bool TryGetToken(Waher.Networking.XMPP.Push.PushMessagingService Source, out string Token);

		/// <summary>
		/// Event raised when a new token is made available.
		/// </summary>
		event TokenEventHandler OnNewToken;
	}
}
