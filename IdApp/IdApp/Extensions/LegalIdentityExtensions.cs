using System;
using System.Linq;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Extensions
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
        /// <returns>If ID needs to be updated</returns>
        public static bool NeedsUpdating(this LegalIdentity legalIdentity)
        {
            return legalIdentity is null ||
                legalIdentity.State == IdentityState.Compromised ||
                legalIdentity.State == IdentityState.Obsoleted ||
                legalIdentity.State == IdentityState.Rejected;
        }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="LegalIdentity"/> is in either of the two states <c>Created</c> or <c>Approved</c>.
        /// </summary>
        /// <param name="legalIdentity">The legal identity whose state to check.</param>
        /// <returns>If an identity is in a Created or Approved state</returns>
        public static bool IsCreatedOrApproved(this LegalIdentity legalIdentity)
        {
            return !(legalIdentity is null) && (legalIdentity.State == IdentityState.Created || legalIdentity.State == IdentityState.Approved);
        }

        /// <summary>
        /// Returns the JID if the <see cref="LegalIdentity"/> has one, or the empty string otherwise.
        /// </summary>
        /// <param name="legalIdentity">The legal identity whose JID to get.</param>
        /// <param name="defaultValueIfNotFound">The default value to use if JID isn't found.</param>
        /// <returns>Gets the JID property of an identity object.</returns>
        public static string GetJid(this LegalIdentity legalIdentity, string defaultValueIfNotFound = "")
        {
            string jid = null;
            if (!(legalIdentity is null) && legalIdentity.Properties?.Length > 0)
            { 
                jid = legalIdentity.Properties.FirstOrDefault(x => x.Name == Constants.XmppProperties.Jid)?.Value;
            }

            return !string.IsNullOrWhiteSpace(jid) ? jid : defaultValueIfNotFound;
        }
    }
}