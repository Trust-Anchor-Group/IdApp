using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	[Singleton]
	public class PushNotificationService : LoadableService, IPushNotificationService
	{
		private readonly Dictionary<Waher.Networking.XMPP.Push.PushMessagingService, string> tokens = new();

		/// <summary>
		/// Push notification service
		/// </summary>
		public PushNotificationService()
		{
		}

		/// <summary>
		/// New token received from push notification back-end.
		/// </summary>
		/// <param name="Source">Source of token.</param>
		/// <param name="Token">Token</param>
		/// <param name="ClientType">Client Type</param>
		public async Task NewToken(Waher.Networking.XMPP.Push.PushMessagingService Source, string Token, ClientType ClientType)
		{
			lock (this.tokens)
			{
				this.tokens[Source] = Token;
			}

			await this.XmppService.NewPushNotificationToken(Token, Source, ClientType);

			try
			{
				this.OnNewToken?.Invoke(this, new TokenEventArgs(Source, Token, ClientType));
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Event raised when a new token is made available.
		/// </summary>
		public event TokenEventHandler OnNewToken;

		/// <summary>
		/// Tries to get a token from a push notification service.
		/// </summary>
		/// <param name="Source">Source of token</param>
		/// <param name="Token">Token, if found.</param>
		/// <returns>If a token was found for the corresponding source.</returns>
		public bool TryGetToken(Waher.Networking.XMPP.Push.PushMessagingService Source, out string Token)
		{
			lock (this.tokens)
			{
				return this.tokens.TryGetValue(Source, out Token);
			}
		}
	}
}
