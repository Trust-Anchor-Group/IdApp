using IdApp.Services.Messages;
using System;
using System.Threading.Tasks;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// Interfaces for views displaying markdown
	/// </summary>
	public interface IChatView
	{
		/// <summary>
		/// Called when a Multi-media URI link using the XMPP URI scheme.
		/// </summary>
		/// <param name="Message">Message containing the URI.</param>
		/// <param name="Uri">URI</param>
		Task ExecuteXmppUriClicked(ChatMessage Message, string Uri);
	}
}
