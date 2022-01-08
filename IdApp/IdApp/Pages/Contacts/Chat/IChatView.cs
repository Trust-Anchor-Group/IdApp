using IdApp.Services.Messages;
using System;
using System.Threading.Tasks;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// URI Scheme
	/// </summary>
	public enum UriScheme
	{
		/// <summary>
		/// XMPP URI Scheme (xmpp)
		/// </summary>
		Xmpp,

		/// <summary>
		/// IoTID URI Scheme (iotid)
		/// </summary>
		IotId
	}

	/// <summary>
	/// Interfaces for views displaying markdown
	/// </summary>
	public interface IChatView
	{
		/// <summary>
		/// Called when a special Multi-media URI link has been clicked.
		/// </summary>
		/// <param name="Message">Message containing the URI.</param>
		/// <param name="Uri">URI</param>
		/// <param name="Scheme">URI Scheme</param>
		Task ExecuteXmppUriClicked(ChatMessage Message, string Uri, UriScheme Scheme);
	}
}
