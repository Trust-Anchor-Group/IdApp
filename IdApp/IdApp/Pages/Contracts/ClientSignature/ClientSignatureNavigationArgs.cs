﻿using IdApp.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Contracts.ClientSignature
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a client signature.
    /// </summary>
    public class ClientSignatureNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ClientSignatureNavigationArgs"/> class.
        /// </summary>
        /// <param name="signature">The signature to display.</param>
        /// <param name="identity">The legal identity to display.</param>
        public ClientSignatureNavigationArgs(Waher.Networking.XMPP.Contracts.ClientSignature signature, LegalIdentity identity)
        {
            this.Signature = signature;
            this.Identity = identity;
        }

        /// <summary>
        /// The signature to display.
        /// </summary>
        public Waher.Networking.XMPP.Contracts.ClientSignature Signature { get; }

        /// <summary>
        /// The legal identity to display.
        /// </summary>
        public LegalIdentity Identity { get; }
    }
}