using System;
using Waher.Networking.XMPP;

namespace Tag.Neuron.Xamarin.Services
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