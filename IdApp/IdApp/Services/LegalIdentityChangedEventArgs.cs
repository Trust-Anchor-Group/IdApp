using System;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services
{
    /// <summary>
    /// Represents the current <see cref="LegalIdentity"/> when changed.
    /// </summary>
    public sealed class LegalIdentityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of a <see cref="LegalIdentityChangedEventArgs"/>.
        /// </summary>
        /// <param name="identity">The changed identity.</param>
        public LegalIdentityChangedEventArgs(LegalIdentity identity)
        {
            Identity = identity;
        }

        /// <summary>
        /// The changed identity.
        /// </summary>
        public LegalIdentity Identity { get; }
    }
}