using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="LegalIdentity"/> class.
    /// </summary>
    public static class LegalIdentityExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if the legal identity is either null or is in a 'bad' state (rejected, compromised or obsolete).
        /// </summary>
        /// <param name="legalIdentity">The legal identity whose state to check.</param>
        /// <returns></returns>
        public static bool NeedsUpdating(this LegalIdentity legalIdentity)
        {
            return legalIdentity == null ||
                legalIdentity.State == IdentityState.Compromised ||
                legalIdentity.State == IdentityState.Obsoleted ||
                legalIdentity.State == IdentityState.Rejected;
        }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="LegalIdentity"/> is in either of the two states <c>Created</c> or <c>Approved</c>.
        /// </summary>
        /// <param name="legalIdentity">The legal identity whose state to check.</param>
        /// <returns></returns>
        public static bool IsCreatedOrApproved(this LegalIdentity legalIdentity)
        {
            return legalIdentity != null && (legalIdentity.State == IdentityState.Created || legalIdentity.State == IdentityState.Approved);
        }
    }
}