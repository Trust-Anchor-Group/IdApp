using System;
using System.Threading.Tasks;

namespace IdApp.Services.Push
{
	/// <summary>
	/// Delegate for token event handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate Task TokenEventHandler(object Sender, TokenEventArgs e);

	/// <summary>
	/// Event argumens for token-based events.
	/// </summary>
	public class TokenEventArgs : EventArgs
	{
		/// <summary>
		/// Event argumens for token-based events.
		/// </summary>
		/// <param name="Source">Source of notification</param>
		/// <param name="Token">Token</param>
		public TokenEventArgs(PushMessagingService Source, string Token)
			: base()
		{
			this.Source = Source;
			this.Token = Token;
		}

		/// <summary>
		/// Source of notification
		/// </summary>
		public PushMessagingService Source { get; }

		/// <summary>
		/// Token
		/// </summary>
		public string Token { get; }
	}
}
