using System;
using Waher.Networking.XMPP;

namespace XamarinApp.Services
{
    public sealed class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStateChangedEventArgs(XmppState state)
        {
            State = state;
        }

        public XmppState State { get; }
    }
}