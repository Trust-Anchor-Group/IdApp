using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Navigation
{
    public class PetitionContractNavigationArgs : NavigationArgs
    {
        public PetitionContractNavigationArgs(
                LegalIdentity requestorIdentity,
                string requestorFullJid,
                Contract requestedContract,
                string petitionId,
                string purpose)
        {
            this.RequestorIdentity = requestorIdentity;
            this.RequestorFullJid = requestorFullJid;
            this.RequestedContract = requestedContract;
            this.PetitionId = petitionId;
            this.Purpose = purpose;
        }

        public LegalIdentity RequestorIdentity { get; }
        public string RequestorFullJid { get; }
        public Contract RequestedContract { get; }
        public string PetitionId { get; }
        public string Purpose { get; }
    }
}