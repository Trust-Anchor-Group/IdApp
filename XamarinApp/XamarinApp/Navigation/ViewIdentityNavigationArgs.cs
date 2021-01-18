using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Navigation
{
    public class ViewIdentityNavigationArgs : NavigationArgs
    {
        public ViewIdentityNavigationArgs(LegalIdentity identity, SignaturePetitionEventArgs identityToReview)
        {
            this.Identity = identity;
            this.IdentityToReview = identityToReview;
        }
        
        public LegalIdentity Identity { get; }
        public SignaturePetitionEventArgs IdentityToReview { get; }
    }
}