using System;
using Waher.Networking.XMPP;

namespace XamarinApp
{
    public interface IConnectionStateChanged
    {
        void ConnectionStateChanged(XmppState NewState);
    }
}
