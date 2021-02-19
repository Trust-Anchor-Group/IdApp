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
        /// <param name="isUserInitiated">Sets whether the user manually initiated a logout (or login).</param>
        public ConnectionStateChangedEventArgs(XmppState state, bool isUserInitiated)
        {
            State = state;
            this.IsUserInitiated = isUserInitiated;
        }

        /// <summary>
        /// The current state.
        /// </summary>
        public XmppState State { get; }

        /// <summary>
        /// Gets whether the user manually initiated a logout (or login).
        /// </summary>
        public bool IsUserInitiated { get; }
    }
}