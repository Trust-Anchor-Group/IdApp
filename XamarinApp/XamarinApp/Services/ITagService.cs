using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Content;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;

namespace XamarinApp.Services
{
    public interface ITagService : IDisposable
    {
        #region Lifecycle

        Task Load();
        Task Unload();
        event EventHandler<LoadedEventArgs> Loaded;
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        #endregion

        XmppClient CreateClient(string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly);

        #region State

        bool IsOnline { get; }
        XmppState State { get; }
        string Domain { get; }
        string Host { get; }
        string BareJID { get; }
        XmppConfiguration Configuration { get; }

        #endregion

        #region Legal Identity

        event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;
        Task AddLegalIdentity(List<Property> properties, params LegalIdentityAttachment[] attachments);
        Task<LegalIdentity[]> GetLegalIdentitiesAsync();
        Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId);
        Task PetitionIdentityAsync(string legalId, string petitionId, string purpose);
        Task PetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response);
        Task PetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response);
        Task PetitionSignatureResponseAsync(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response);
        Task PetitionPeerReviewIDAsync(string legalId, LegalIdentity identity, string petitionId, string purpose);
        Task<byte[]> SignAsync(byte[] data);
        bool HasLegalIdentityAttachments { get; }
        Attachment[] GetLegalIdentityAttachments();
        bool LegalIdentityIsValid { get; }
        Task<LegalIdentity> ObsoleteLegalIdentityAsync(string legalIdentityId);
        Task<LegalIdentity> CompromisedLegalIdentityAsync(string legalIdentityId);

        #endregion

        #region Contracts

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
        Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url);
        Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url, TimeSpan timeout);

        #endregion

        #region Configuration

        bool FileUploadIsSupported { get; }
        bool PinIsValid { get; }
        void SetPin(string pin, bool usePin);
        void ResetPin();
        void SetAccount(string accountNameText, string clientPasswordHash, string clientPasswordHashMethod);
        void SetDomain(string domainName, string legalJid);

        #endregion

        Task UpdateXmpp();
        Task<bool> CheckServices();
        Task<bool> FindServices(XmppClient client = null);
        Task<(string hostName, int port)> GetXmppHostnameAndPort(string domainName = null);
        
        void DecrementConfigurationStep(int? stepToRevertTo = null);
        void IncrementConfigurationStep();
        void UpdateConfiguration();
    }
}