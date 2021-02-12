using System;
using Waher.Networking.XMPP;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Represents an event class holding data related to connection state changes.
    /// </summary>
    public sealed class ConnectionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="state">The current state.</param>
        public ConnectionStateChangedEventArgs(XmppState state)
        {
            State = state;
        }

        /// <summary>
        /// The current state.
        /// </summary>
        public XmppState State { get; }
    }
}