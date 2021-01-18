using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Navigation
{
    public class ClientSignatureNavigationArgs : NavigationArgs
    {
        public ClientSignatureNavigationArgs(ClientSignature signature, LegalIdentity identity)
        {
            this.Signature = signature;
            this.Identity = identity;
        }

        public ClientSignature Signature { get; }
        public LegalIdentity Identity { get; }
    }
}