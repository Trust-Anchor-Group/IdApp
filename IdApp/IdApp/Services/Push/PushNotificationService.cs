using System;
using System.Collections.Generic;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	[Singleton]
	public class PushNotificationService : IPushNotificationService
	{
		private readonly Dictionary<PushMessagingService, string> tokens = new();

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
		public void NewToken(PushMessagingService Source, string Token)
		{
			lock (this.tokens)
			{
				this.tokens[Source] = Token;
			}

			try
			{
				this.OnNewToken?.Invoke(this, new TokenEventArgs(Source, Token));
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
		public bool TryGetToken(PushMessagingService Source, out string Token)
		{
			lock (this.tokens)
			{
				return this.tokens.TryGetValue(Source, out Token);
			}
		}
	}
}
