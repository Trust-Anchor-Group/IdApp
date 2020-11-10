using System;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Services
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