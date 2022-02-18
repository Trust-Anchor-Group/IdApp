using Waher.Runtime.Inventory;

namespace IdApp.Services.Xmpp
{
    [Singleton]
    internal sealed class XmppMultiUserChat : ServiceReferences, IXmppMultiUserChat
    {
        internal XmppMultiUserChat()
            : base()
        {
        }
    }
}