using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Content;
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

        Task<(bool succeeded, string errorMessage)> TryConnect(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);
        Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);
        Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, string hostName, int portNumber, string accountName, string passwordHash, string passwordHashMethod, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

        #region State

        bool IsOnline { get; }
        XmppState State { get; }
        //string Domain { get; }
        //string Account { get; }
        //string Host { get; }
        string BareJId { get; }

        #endregion

        #region Legal Identity

        event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;
        Task<LegalIdentity> AddLegalIdentityAsync(List<Property> properties, params LegalIdentityAttachment[] attachments);
        Task<LegalIdentity[]> GetLegalIdentitiesAsync();
        Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId);
        Task PetitionIdentityAsync(string legalId, string petitionId, string purpose);
        Task SendPetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response);
        Task SendPetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response);
        Task SendPetitionSignatureResponseAsync(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response);
        Task PetitionPeerReviewIDAsync(string legalId, LegalIdentity identity, string petitionId, string purpose);
        Task<byte[]> SignAsync(byte[] data);
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

        #endregion

        Task<bool> DiscoverServices(XmppClient client = null);
    }
}