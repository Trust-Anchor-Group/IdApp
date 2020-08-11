using System;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp
{
    public interface ILegalIdentityChanged
    {
        void LegalIdentityChanged(LegalIdentity Identity);
    }
}
