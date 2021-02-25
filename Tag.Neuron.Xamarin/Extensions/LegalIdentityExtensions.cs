using System;
using System.Linq;
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

        /// <summary>
        /// Returns the JID if the <see cref="LegalIdentity"/> has one, or the empty string otherwise.
        /// </summary>
        /// <param name="legalIdentity">The legal identity whose JID to get.</param>
        /// <param name="defaultValueIfNotFound">The default value to use if JID isn't found.</param>
        /// <returns></returns>
        public static string GetJId(this LegalIdentity legalIdentity, string defaultValueIfNotFound = "")
        {
            string jid = null;
            if (legalIdentity != null && legalIdentity.Properties?.Length > 0)
            { 
                jid = legalIdentity.Properties.FirstOrDefault(x => x.Name == Constants.XmppProperties.JId)?.Value;
            }

            return !string.IsNullOrWhiteSpace(jid) ? jid : defaultValueIfNotFound;
        }
    }
}