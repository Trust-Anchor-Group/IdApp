using Waher.Runtime.Inventory;

namespace IdApp.Services.Xmpp
{
    /// <summary>
    /// Adds support for Xmpp Multi-User Chat functionality.
    /// </summary>
    [DefaultImplementation(typeof(XmppMultiUserChat))]
    public interface IXmppMultiUserChat
    {
    }
}