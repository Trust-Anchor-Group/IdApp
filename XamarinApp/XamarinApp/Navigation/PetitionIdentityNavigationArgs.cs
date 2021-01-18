using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Navigation
{
    public class PetitionIdentityNavigationArgs : NavigationArgs
    {
        public PetitionIdentityNavigationArgs(
                LegalIdentity requestorIdentity,
                string requestorFullJid,
                string requestedIdentityId,
                string petitionId,
                string purpose)
        {
            this.RequestorIdentity = requestorIdentity;
            this.RequestorFullJid = requestorFullJid;
            this.RequestedIdentityId = requestedIdentityId;
            this.PetitionId = petitionId;
            this.Purpose = purpose;
        }

        public LegalIdentity RequestorIdentity { get; }
        public string RequestorFullJid { get; }
        public string RequestedIdentityId { get; }
        public string PetitionId { get; }
        public string Purpose { get; }
    }
}