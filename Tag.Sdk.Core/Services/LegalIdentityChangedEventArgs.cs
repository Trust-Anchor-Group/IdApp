using System;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Sdk.Core.Services
{
    public sealed class LegalIdentityChangedEventArgs : EventArgs
    {
        public LegalIdentityChangedEventArgs(LegalIdentity identity)
        {
            Identity = identity;
        }

        public LegalIdentity Identity { get; }
    }
}