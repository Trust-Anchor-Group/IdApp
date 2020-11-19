using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Extensions
{
    public static class LegalIdentityExtensions
    {
        public static bool NeedsUpdating(this LegalIdentity legalIdentity)
        {
            return legalIdentity == null ||
                legalIdentity.State == IdentityState.Compromised ||
                legalIdentity.State == IdentityState.Obsoleted ||
                legalIdentity.State == IdentityState.Rejected;
        }

        public static bool IsCreatedOrApproved(this LegalIdentity legalIdentity)
        {
            return legalIdentity != null && (legalIdentity.State == IdentityState.Created || legalIdentity.State == IdentityState.Approved);
        }
    }
}