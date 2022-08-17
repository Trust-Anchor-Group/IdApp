using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdApp.Pages.Registration.RegisterIdentity;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;

namespace IdApp.Services.Contracts
{
	/// <summary>
	/// Adds support for legal identities, smart contracts and signatures to an XMPP Service.
	/// </summary>
	[DefaultImplementation(typeof(SmartContracts))]
    public interface ISmartContracts
    {
        #region General

        /// <summary>
        /// Contracts client
        /// </summary>
        ContractsClient ContractsClient { get; }

        /// <summary>
        /// Returns <c>true</c> if file upload is supported, <c>false</c> otherwise.
        /// </summary>
        bool FileUploadIsSupported { get; }

        /// <summary>
        /// HTTP File Upload client
        /// </summary>
        HttpFileUploadClient FileUploadClient { get; }

        #endregion

        #region Legal Identities

        /// <summary>
        /// Adds a legal identity.
        /// </summary>
        /// <param name="Model">The model holding all the values needed.</param>
        /// <param name="Attachments">The physical attachments to upload.</param>
        /// <returns>Legal Identity</returns>
        Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel Model, params LegalIdentityAttachment[] Attachments);

        /// <summary>
        /// Returns a list of legal identities.
        /// </summary>
        /// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
        /// <returns>Legal Identities</returns>
        Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null);

        /// <summary>
        /// Gets a specific legal identity.
        /// </summary>
        /// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
        /// <returns>Legal identity object</returns>
        Task<LegalIdentity> GetLegalIdentity(CaseInsensitiveString legalIdentityId);

        /// <summary>
        /// Checks if a legal identity is in the contacts list.
        /// </summary>
        /// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
        /// <returns>If the legal identity is in the contacts list.</returns>
        Task<bool> IsContact(CaseInsensitiveString legalIdentityId);

        /// <summary>
        /// Checks if the client has access to the private keys of the specified legal identity.
        /// </summary>
        /// <param name="legalIdentityId">The id of the legal identity.</param>
        /// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
        /// <returns>If private keys are available.</returns>
        Task<bool> HasPrivateKey(CaseInsensitiveString legalIdentityId, XmppClient client = null);

        /// <summary>
        /// Marks the legal identity as obsolete.
        /// </summary>
        /// <param name="legalIdentityId">The id to mark as obsolete.</param>
        /// <returns>Legal Identity</returns>
        Task<LegalIdentity> ObsoleteLegalIdentity(CaseInsensitiveString legalIdentityId);

        /// <summary>
        /// Marks the legal identity as compromised.
        /// </summary>
        /// <param name="legalIdentityId">The legal id to mark as compromised.</param>
        /// <returns>Legal Identity</returns>
        Task<LegalIdentity> CompromiseLegalIdentity(CaseInsensitiveString legalIdentityId);

        /// <summary>
        /// Petitions a legal identity.
        /// </summary>
        /// <param name="LegalId">The id of the legal identity.</param>
        /// <param name="PetitionId">The petition id.</param>
        /// <param name="Purpose">The purpose of the petitioning.</param>
        Task PetitionIdentity(CaseInsensitiveString LegalId, string PetitionId, string Purpose);

        /// <summary>
        /// Sends a response to a petitioning identity request.
        /// </summary>
        /// <param name="LegalId">The id of the legal identity.</param>
        /// <param name="PetitionId">The petition id.</param>
        /// <param name="RequestorFullJid">The full Jid of the requestor.</param>
        /// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
        Task SendPetitionIdentityResponse(CaseInsensitiveString LegalId, string PetitionId, string RequestorFullJid, bool Response);

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

        #endregion

        #region Smart Contracts

        /// <summary>
        /// Gets the contract with the specified id.
        /// </summary>
        /// <param name="ContractId">The contract id.</param>
        /// <returns>Smart Contract</returns>
        Task<Contract> GetContract(CaseInsensitiveString ContractId);
        
        /// <summary>
        /// Gets references to created contracts.
        /// </summary>
        /// <returns>Created contracts.</returns>
        Task<string[]> GetCreatedContractReferences();

		/// <summary>
		/// Gets references to signed contracts.
		/// </summary>
		/// <returns>Signed contracts.</returns>
		Task<string[]> GetSignedContractReferences();

        /// <summary>
        /// Signs a given contract.
        /// </summary>
        /// <param name="Contract">The contract to sign.</param>
        /// <param name="Role">The role of the signer.</param>
        /// <param name="Transferable">Whether the contract is transferable or not.</param>
        /// <returns>Smart Contract</returns>
        Task<Contract> SignContract(Contract Contract, string Role, bool Transferable);

        /// <summary>
        /// Obsoletes a contract.
        /// </summary>
        /// <param name="ContractId">The id of the contract to obsolete.</param>
        /// <returns>Smart Contract</returns>
        Task<Contract> ObsoleteContract(CaseInsensitiveString ContractId);

        /// <summary>
        /// Creates a new contract.
        /// </summary>
        /// <param name="TemplateId">The id of the contract template to use.</param>
        /// <param name="Parts">The individual contract parts.</param>
        /// <param name="Parameters">Contract parameters.</param>
        /// <param name="Visibility">The contract's visibility.</param>
        /// <param name="PartsMode">The contract's parts.</param>
        /// <param name="Duration">Duration of the contract.</param>
        /// <param name="ArchiveRequired">Required duration for contract archival.</param>
        /// <param name="ArchiveOptional">Optional duration for contract archival.</param>
        /// <param name="SignAfter">Timestamp of when the contract can be signed at the earliest.</param>
        /// <param name="SignBefore">Timestamp of when the contract can be signed at the latest.</param>
        /// <param name="CanActAsTemplate">Can this contract act as a template itself?</param>
        /// <returns>Smart Contract</returns>
        Task<Contract> CreateContract(
            CaseInsensitiveString TemplateId,
            Part[] Parts,
            Parameter[] Parameters,
            ContractVisibility Visibility,
            ContractParts PartsMode,
            Duration Duration,
            Duration ArchiveRequired,
            Duration ArchiveOptional,
            DateTime? SignAfter,
            DateTime? SignBefore,
            bool CanActAsTemplate);

        /// <summary>
        /// Deletes a contract.
        /// </summary>
        /// <param name="ContractId">The id of the contract to delete.</param>
        /// <returns>Smart Contract</returns>
        Task<Contract> DeleteContract(CaseInsensitiveString ContractId);

        /// <summary>
        /// Petitions a contract with the specified id and purpose.
        /// </summary>
        /// <param name="ContractId">The contract id.</param>
        /// <param name="PetitionId">The petition id.</param>
        /// <param name="Purpose">The purpose.</param>
        Task PetitionContract(CaseInsensitiveString ContractId, string PetitionId, string Purpose);

        /// <summary>
        /// Sends a response to a petitioning contract request.
        /// </summary>
        /// <param name="ContractId">The id of the contract.</param>
        /// <param name="PetitionId">The petition id.</param>
        /// <param name="RequestorFullJid">The full Jid of the requestor.</param>
        /// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
        Task SendPetitionContractResponse(CaseInsensitiveString ContractId, string PetitionId, string RequestorFullJid, bool Response);

        /// <summary>
        /// An event that fires when a petition for a contract is received.
        /// </summary>
        event EventHandler<ContractPetitionEventArgs> PetitionForContractReceived;

        /// <summary>
        /// An event that fires when a petitioned contract response is received.
        /// </summary>
        event EventHandler<ContractPetitionResponseEventArgs> PetitionedContractResponseReceived;

        /// <summary>
        /// Gets the timestamp of the last event received for a given contract ID.
        /// </summary>
        /// <param name="ContractId">Contract ID</param>
        /// <returns>Timestamp</returns>
        DateTime GetTimeOfLastContractEvent(CaseInsensitiveString ContractId);

        #endregion

        #region Attachments

        /// <summary>
        /// Gets an attachment for a contract.
        /// </summary>
        /// <param name="Url">The url of the attachment.</param>
        /// <param name="Timeout">Max timeout allowed when retrieving an attachment.</param>
        /// <param name="SignWith">How the request is signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
        /// <returns>Content-Type, and attachment file.</returns>
        Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string Url, SignWith SignWith, TimeSpan Timeout);

        #endregion

        #region Peer Review

        /// <summary>
        /// Sends a petition to a third-party to review a legal identity.
        /// </summary>
        /// <param name="LegalId">The legal id to petition.</param>
        /// <param name="Identity">The legal id to peer review.</param>
        /// <param name="PetitionId">The petition id.</param>
        /// <param name="Purpose">The purpose.</param>
        Task PetitionPeerReviewId(CaseInsensitiveString LegalId, LegalIdentity Identity, string PetitionId, string Purpose);

        /// <summary>
        /// Adds an attachment for the peer review.
        /// </summary>
        /// <param name="Identity">The identity to which the attachment should be added.</param>
        /// <param name="ReviewerLegalIdentity">The identity of the reviewer.</param>
        /// <param name="PeerSignature">The raw signature data.</param>
        /// <returns>Legal Identity</returns>
        Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity Identity, LegalIdentity ReviewerLegalIdentity, byte[] PeerSignature);

        /// <summary>
        /// An event that fires when a petition for peer review is received.
        /// </summary>
        event EventHandler<SignaturePetitionEventArgs> PetitionForPeerReviewIdReceived;

        /// <summary>
        /// An event that fires when a petitioned peer review response is received.
        /// </summary>
        event EventHandler<SignaturePetitionResponseEventArgs> PetitionedPeerReviewIdResponseReceived;

		/// <summary>
		/// Event raised when a response to a signature petition has been received.
		/// </summary>
		event EventHandler<SignaturePetitionResponseEventArgs> SignaturePetitionResponseReceived;

		/// <summary>
		/// Event raised when a contract proposal has been received.
		/// </summary>
		event EventHandler<ContractProposalEventArgs> ContractProposalReceived;

		/// <summary>
		/// Event raised when contract was updated.
		/// </summary>
		public event EventHandler<ContractReferenceEventArgs> ContractUpdated;

		/// <summary>
		/// Event raised when contract was signed.
		/// </summary>
		public event EventHandler<ContractSignedEventArgs> ContractSigned;

		#endregion

		#region Signatures

		/// <summary>
		/// Signs binary data with the corresponding private key.
		/// </summary>
		/// <param name="data">The data to sign.</param>
		/// <param name="signWith">What keys that can be used to sign the data.</param>
		/// <returns>Signature</returns>
		Task<byte[]> Sign(byte[] data, SignWith signWith);

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
        /// Sends a response to a petitioning signature request.
        /// </summary>
        /// <param name="LegalId">Legal Identity petitioned.</param>
        /// <param name="Content">Content to be signed.</param>
        /// <param name="Signature">Digital signature of content, made by the legal identity.</param>
        /// <param name="PetitionId">A petition identifier. This identifier will follow the petition, and can be used
        /// to identify the petition request.</param>
        /// <param name="RequestorFullJid">Full JID of requestor.</param>
        /// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
        Task SendPetitionSignatureResponse(CaseInsensitiveString LegalId, byte[] Content, byte[] Signature, string PetitionId, string RequestorFullJid, bool Response);

        /// <summary>
        /// An event that fires when a petition for a signature is received.
        /// </summary>
        event EventHandler<SignaturePetitionEventArgs> PetitionForSignatureReceived;

        #endregion
    }
}
