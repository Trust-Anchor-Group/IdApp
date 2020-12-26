using System;
using Waher.Networking.XMPP;

namespace Tag.Sdk.Core.Services
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