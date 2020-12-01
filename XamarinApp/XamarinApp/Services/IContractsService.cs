using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Models;

namespace XamarinApp.Services
{
    public interface IContractsService : IDisposable
    {
        Task PetitionContractAsync(string contractId, string petitionId, string purpose);
        Task<Contract> GetContractAsync(string contractId);
        Task<string[]> GetCreatedContractsAsync();
        Task<string[]> GetSignedContractsAsync();
        Task<Contract> SignContractAsync(Contract contract, string role, bool transferable);
        Task<Contract> ObsoleteContractAsync(string contractId);
        Task<Contract> CreateContractAsync(
            string templateId,
            Part[] parts,
            Parameter[] parameters,
            ContractVisibility visibility,
            ContractParts partsMode,
            Duration duration,
            Duration archiveRequired,
            Duration archiveOptional,
            DateTime? signAfter,
            DateTime? signBefore,
            bool canActAsTemplate);
        Task<Contract> DeleteContractAsync(string contractId);
        Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url, TimeSpan timeout);

        Task<LegalIdentity> AddLegalIdentityAsync(RegisterIdentityModel model, params LegalIdentityAttachment[] attachments);
        Task<LegalIdentity[]> GetLegalIdentitiesAsync();
        Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId);
        Task PetitionIdentityAsync(string legalId, string petitionId, string purpose);
        Task SendPetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response);
        Task SendPetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response);
        Task SendPetitionSignatureResponseAsync(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response);
        Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity identity, LegalIdentity reviewerLegalIdentity, byte[] peerSignature);
        Task PetitionPeerReviewIdAsync(string legalId, LegalIdentity identity, string petitionId, string purpose);
        Task<byte[]> SignAsync(byte[] data);
        bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature);
        Task<LegalIdentity> ObsoleteLegalIdentityAsync(string legalIdentityId);
        Task<LegalIdentity> CompromisedLegalIdentityAsync(string legalIdentityId);

        bool FileUploadIsSupported { get; }

        event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;
        event EventHandler<LegalIdentityPetitionEventArgs> PetitionForIdentityReceived;
        event EventHandler<LegalIdentityPetitionResponseEventArgs> PetitionedIdentityResponseReceived;
        event EventHandler<ContractPetitionEventArgs> PetitionForContractReceived;
        event EventHandler<ContractPetitionResponseEventArgs> PetitionedContractResponseReceived;
        event EventHandler<SignaturePetitionEventArgs> PetitionForPeerReviewIdReceived;
        event EventHandler<SignaturePetitionResponseEventArgs> PetitionedPeerReviewIdResponseReceived;
        event EventHandler<SignaturePetitionEventArgs> PetitionForSignatureReceived;
    }
}