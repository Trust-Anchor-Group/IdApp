using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Models;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for legal identities, smart contracts and signatures to a Neuron Service.
    /// </summary>
    [DefaultImplementation(typeof(NeuronContracts))]
    public interface INeuronContracts : IDisposable
    {
        /// <summary>
        /// An event to monitor a Neuron service's Contracts for connectivity.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Petitions a contract with the specified id and purpose.
        /// </summary>
        /// <param name="contractId">The contract id.</param>
        /// <param name="petitionId">The petition id.</param>
        /// <param name="purpose">The purpose.</param>
        /// <returns></returns>
        Task PetitionContract(string contractId, string petitionId, string purpose);
        
        /// <summary>
        /// Gets the contract with the specified id.
        /// </summary>
        /// <param name="contractId">The contract id.</param>
        /// <returns></returns>
        Task<Contract> GetContract(string contractId);
        
        /// <summary>
        /// Gets the id's of the created contracts.
        /// </summary>
        /// <returns>Id's of the created contracts.</returns>
        Task<string[]> GetCreatedContractIds();
        
        /// <summary>
        /// Gets the id's of the signed contracts.
        /// </summary>
        /// <returns>Id's of the signed contracts.</returns>
        Task<string[]> GetSignedContractIds();
        
        /// <summary>
        /// Signs a given contract.
        /// </summary>
        /// <param name="contract">The contract to sign.</param>
        /// <param name="role">The role of the signer.</param>
        /// <param name="transferable">Whether the contract is transferable or not.</param>
        /// <returns></returns>
        Task<Contract> SignContract(Contract contract, string role, bool transferable);
        
        /// <summary>
        /// Obsoletes a contract.
        /// </summary>
        /// <param name="contractId">The id of the contract to obsolete.</param>
        /// <returns></returns>
        Task<Contract> ObsoleteContract(string contractId);
        
        /// <summary>
        /// Creates a new contract.
        /// </summary>
        /// <param name="templateId">The id of the contract template to use.</param>
        /// <param name="parts">The individual contract parts.</param>
        /// <param name="parameters">Contract parameters.</param>
        /// <param name="visibility">The contract's visibility.</param>
        /// <param name="partsMode">The contract's parts.</param>
        /// <param name="duration">Duration of the contract.</param>
        /// <param name="archiveRequired">Required duration for contract archival.</param>
        /// <param name="archiveOptional">Optional duration for contract archival.</param>
        /// <param name="signAfter">Timestamp of when the contract can be signed at the earliest.</param>
        /// <param name="signBefore">Timestamp of when the contract can be signed at the latest.</param>
        /// <param name="canActAsTemplate">Can this contract act as a template itself?</param>
        /// <returns></returns>
        Task<Contract> CreateContract(
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
        
        /// <summary>
        /// Deletes a contract.
        /// </summary>
        /// <param name="contractId">The id of the contract to delete.</param>
        /// <returns></returns>
        Task<Contract> DeleteContract(string contractId);
        
        /// <summary>
        /// Gets an attachment for a contract.
        /// </summary>
        /// <param name="url">The url of the attachment.</param>
        /// <param name="timeout">Max timeout allowed when retrieving an attachment.</param>
        /// <returns></returns>
        
        Task<KeyValuePair<string, TemporaryFile>> GetContractAttachment(string url, TimeSpan timeout);
        /// <summary>
        /// Adds a legal identity.
        /// </summary>
        /// <param name="model">The model holding all the values needed.</param>
        /// <param name="attachments">The physical attachments to upload.</param>
        /// <returns></returns>
        Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel model, params LegalIdentityAttachment[] attachments);
        
        /// <summary>
        /// Returns a list of legal identities.
        /// </summary>
        /// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
        /// <returns></returns>
        Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null);
        
        /// <summary>
        /// Gets a specific legal identity.
        /// </summary>
        /// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
        /// <returns></returns>
        Task<LegalIdentity> GetLegalIdentity(string legalIdentityId);

        /// <summary>
        /// Checks if the client has access to the private keys of the specified legal identity.
        /// </summary>
        /// <param name="legalIdentityId">The id of the legal identity.</param>
        /// <returns>If private keys are available.</returns>
        Task<bool> HasPrivateKey(string legalIdentityId);

        /// <summary>
        /// Petitions a legal identity.
        /// </summary>
        /// <param name="legalId">The id of the legal identity.</param>
        /// <param name="petitionId">The petition id.</param>
        /// <param name="purpose">The purpose of the petitioning.</param>
        /// <returns></returns>
        Task PetitionIdentity(string legalId, string petitionId, string purpose);
        
        /// <summary>
        /// Sends a response to a petitioning identity request.
        /// </summary>
        /// <param name="legalId">The id of the legal identity.</param>
        /// <param name="petitionId">The petition id.</param>
        /// <param name="requestorFullJid">The full Jid of the requestor.</param>
        /// <param name="response">If the petition is accepted (true) or rejected (false).</param>
        /// <returns></returns>
        Task SendPetitionIdentityResponse(string legalId, string petitionId, string requestorFullJid, bool response);
        
        /// <summary>
        /// Sends a response to a petitioning contract request.
        /// </summary>
        /// <param name="contractId">The id of the contract.</param>
        /// <param name="petitionId">The petition id.</param>
        /// <param name="requestorFullJid">The full Jid of the requestor.</param>
        /// <param name="response">If the petition is accepted (true) or rejected (false).</param>
        /// <returns></returns>
        Task SendPetitionContractResponse(string contractId, string petitionId, string requestorFullJid, bool response);
        
        /// <summary>
        /// Sends a response to a petitioning signature request.
        /// </summary>
        /// <param name="legalId">Legal Identity petitioned.</param>
        /// <param name="content">Content to be signed.</param>
        /// <param name="signature">Digital signature of content, made by the legal identity.</param>
        /// <param name="petitionId">A petition identifier. This identifier will follow the petition, and can be used
        /// to identify the petition request.</param>
        /// <param name="requestorFullJid">Full JID of requestor.</param>
        /// <param name="response">If the petition is accepted (true) or rejected (false).</param>
        /// <returns></returns>
        Task SendPetitionSignatureResponse(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response);
        
        /// <summary>
        /// Adds an attachment for the peer review.
        /// </summary>
        /// <param name="identity">The identity to which the attachment should be added.</param>
        /// <param name="reviewerLegalIdentity">The identity of the reviewer.</param>
        /// <param name="peerSignature">The raw signature data.</param>
        /// <returns></returns>
        Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity identity, LegalIdentity reviewerLegalIdentity, byte[] peerSignature);
        
        /// <summary>
        /// Sends a petition to a third-party to review a legal identity.
        /// </summary>
        /// <param name="legalId">The legal id to petition.</param>
        /// <param name="identity">The legal id to peer review.</param>
        /// <param name="petitionId">The petition id.</param>
        /// <param name="purpose">The purpose.</param>
        /// <returns></returns>
        Task PetitionPeerReviewId(string legalId, LegalIdentity identity, string petitionId, string purpose);
        
        /// <summary>
        /// Signs binary data with the corresponding private key.
        /// </summary>
        /// <param name="data">The data to sign.</param>
		/// <param name="SignWith">What keys that can be used to sign the data.</param>
        /// <returns></returns>
        Task<byte[]> Sign(byte[] data, SignWith SignWith);
        
        /// <summary>Validates a signature of binary data.</summary>
        /// <param name="legalIdentity">Legal identity used to create the signature.</param>
        /// <param name="data">Binary data to sign-</param>
        /// <param name="signature">Digital signature of data</param>
        /// <returns>
        /// true = Signature is valid.
        /// false = Signature is invalid.
        /// null = Client key algorithm is unknown, and veracity of signature could not be established.
        /// </returns>
        bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature);
        
        /// <summary>
        /// Marks the legal identity as obsolete.
        /// </summary>
        /// <param name="legalIdentityId">The id to mark as obsolete.</param>
        /// <returns></returns>
        Task<LegalIdentity> ObsoleteLegalIdentity(string legalIdentityId);
        
        /// <summary>
        /// Marks the legal identity as compromised.
        /// </summary>
        /// <param name="legalIdentityId">The legal id to mark as compromised.</param>
        /// <returns></returns>
        Task<LegalIdentity> CompromiseLegalIdentity(string legalIdentityId);

        /// <summary>
        /// Returns <c>true</c> if file upload is supported, <c>false</c> otherwise.
        /// </summary>
        bool FileUploadIsSupported { get; }
        
        /// <summary>
        /// Returns <c>true</c> if contracts is online, <c>false</c> otherwise.
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// An event that fires when a legal identity changes.
        /// </summary>
        event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;
        
        /// <summary>
        /// An event that fires when a petition for an identity is received.
        /// </summary>
        event EventHandler<LegalIdentityPetitionEventArgs> PetitionForIdentityReceived;
        
        /// <summary>
        /// An event that fires when a petitioned identity response is received.
        /// </summary>
        event EventHandler<LegalIdentityPetitionResponseEventArgs> PetitionedIdentityResponseReceived;
        
        /// <summary>
        /// An event that fires when a petition for a contract is received.
        /// </summary>
        event EventHandler<ContractPetitionEventArgs> PetitionForContractReceived;
        
        /// <summary>
        /// An event that fires when a petitioned contract response is received.
        /// </summary>
        event EventHandler<ContractPetitionResponseEventArgs> PetitionedContractResponseReceived;
        
        /// <summary>
        /// An event that fires when a petition for peer review is received.
        /// </summary>
        event EventHandler<SignaturePetitionEventArgs> PetitionForPeerReviewIdReceived;
        
        /// <summary>
        /// An event that fires when a petitioned peer review response is received.
        /// </summary>
        event EventHandler<SignaturePetitionResponseEventArgs> PetitionedPeerReviewIdResponseReceived;
        
        /// <summary>
        /// An event that fires when a petition for a signature is received.
        /// </summary>
        event EventHandler<SignaturePetitionEventArgs> PetitionForSignatureReceived;
    }
}