using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Models;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal sealed class NeuronContracts : INeuronContracts
    {
        private readonly ITagProfile tagProfile;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IInternalNeuronService neuronService;
        private readonly ILogService logService;
        private ContractsClient contractsClient;
        private HttpFileUploadClient fileUploadClient;

        internal NeuronContracts(ITagProfile tagProfile, IUiDispatcher uiDispatcher, IInternalNeuronService neuronService, ILogService logService)
        {
            this.tagProfile = tagProfile;
            this.uiDispatcher = uiDispatcher;
            this.neuronService = neuronService;
            this.logService = logService;
        }

        public void Dispose()
        {
            this.DestroyClients();
        }

        internal async Task CreateClients()
        {
            await CreateFileUploadClient();
            await CreateContractsClient();
        }

        internal void DestroyClients()
        {
            DestroyFileUploadClient();
            DestroyContractsClient();
        }

        private async Task CreateContractsClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
            {
                DestroyContractsClient();
                XmppState stateBefore = GetState();
                this.contractsClient = await this.neuronService.CreateContractsClientAsync();
                this.contractsClient.IdentityUpdated += ContractsClient_IdentityUpdated;
                this.contractsClient.PetitionForIdentityReceived += ContractsClient_PetitionForIdentityReceived;
                this.contractsClient.PetitionedIdentityResponseReceived += ContractsClient_PetitionedIdentityResponseReceived;
                this.contractsClient.PetitionForContractReceived += ContractsClient_PetitionForContractReceived;
                this.contractsClient.PetitionedContractResponseReceived += ContractsClient_PetitionedContractResponseReceived;
                this.contractsClient.PetitionForSignatureReceived += ContractsClient_PetitionForSignatureReceived;
                this.contractsClient.PetitionedSignatureResponseReceived += ContractsClient_PetitionedSignatureResponseReceived;
                this.contractsClient.PetitionForPeerReviewIDReceived += ContractsClient_PetitionForPeerReviewIdReceived;
                this.contractsClient.PetitionedPeerReviewIDResponseReceived += ContractsClient_PetitionedPeerReviewIdResponseReceived;
                XmppState stateAfter = GetState();
                if (stateBefore != stateAfter)
                {
                    this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(stateAfter, false));
                }
            }
        }

        private void DestroyContractsClient()
        {
            XmppState stateBefore = GetState();
            if (this.contractsClient != null)
            {
                this.contractsClient.IdentityUpdated -= ContractsClient_IdentityUpdated;
                this.contractsClient.PetitionForIdentityReceived -= ContractsClient_PetitionForIdentityReceived;
                this.contractsClient.PetitionedIdentityResponseReceived -= ContractsClient_PetitionedIdentityResponseReceived;
                this.contractsClient.PetitionForContractReceived -= ContractsClient_PetitionForContractReceived;
                this.contractsClient.PetitionedContractResponseReceived -= ContractsClient_PetitionedContractResponseReceived;
                this.contractsClient.PetitionForSignatureReceived -= ContractsClient_PetitionForSignatureReceived;
                this.contractsClient.PetitionedSignatureResponseReceived -= ContractsClient_PetitionedSignatureResponseReceived;
                this.contractsClient.PetitionForPeerReviewIDReceived -= ContractsClient_PetitionForPeerReviewIdReceived;
                this.contractsClient.PetitionedPeerReviewIDResponseReceived -= ContractsClient_PetitionedPeerReviewIdResponseReceived;
                this.contractsClient.Dispose();
                this.contractsClient = null;
            }
            XmppState stateAfter = GetState();
            if (stateBefore != stateAfter)
            {
                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(stateAfter, false));
            }
        }

        private async Task CreateFileUploadClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.HttpFileUploadJid) && this.tagProfile.HttpFileUploadMaxSize.HasValue)
            {
                this.fileUploadClient = await this.neuronService.CreateFileUploadClientAsync();
            }
        }

        private void DestroyFileUploadClient()
        {
            fileUploadClient?.Dispose();
            fileUploadClient = null;
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        private XmppState GetState()
        {
            return this.contractsClient != null ? XmppState.Connected : XmppState.Offline;
        }

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }

        public Task PetitionContract(string contractId, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionContractAsync(contractId, petitionId, purpose);
        }

        public Task<Contract> GetContract(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetContractAsync(contractId);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetAttachmentAsync(url, SignWith.LatestApprovedId);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string url, SignWith signWith, TimeSpan timeout)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetAttachmentAsync(url, signWith, (int)timeout.TotalMilliseconds);
        }

        public Task<Contract> CreateContract(
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
            bool canActAsTemplate)
        {
            AssertContractsIsAvailable();
            return contractsClient.CreateContractAsync(templateId, parts, parameters, visibility, partsMode, duration, archiveRequired, archiveOptional, signAfter, signBefore, canActAsTemplate);
        }

        public Task<Contract> DeleteContract(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.DeleteContractAsync(contractId);
        }

        public Task<string[]> GetCreatedContractIds()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetCreatedContractsAsync();
        }

        public Task<string[]> GetSignedContractIds()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetSignedContractsAsync();
        }

        public Task<Contract> SignContract(Contract contract, string role, bool transferable)
        {
            AssertContractsIsAvailable();
            return contractsClient.SignContractAsync(contract, role, transferable);
        }

        public Task<Contract> ObsoleteContract(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.ObsoleteContractAsync(contractId);
        }

        public async Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel model, params LegalIdentityAttachment[] attachments)
        {
            AssertContractsIsAvailable();
            AssertFileUploadIsAvailable();

            await contractsClient.GenerateNewKeys();

            LegalIdentity identity = await contractsClient.ApplyAsync(model.ToProperties(this.neuronService));

            foreach (var a in attachments)
            {
                HttpFileUploadEventArgs e2 = await fileUploadClient.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
                if (!e2.Ok)
                {
                    throw new Exception(e2.ErrorText);
                }

                await e2.PUT(a.Data, a.ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

                byte[] signature = await contractsClient.SignAsync(a.Data, SignWith.CurrentKeys);

                identity = await contractsClient.AddLegalIdAttachmentAsync(identity.Id, e2.GetUrl, signature);
            }

            return identity;
        }

        /// <summary>
        /// Checks if the client has access to the private keys of the specified legal identity.
        /// </summary>
        /// <param name="legalIdentityId">The id of the legal identity.</param>
        /// <returns>If private keys are available.</returns>
        public Task<bool> HasPrivateKey(string legalIdentityId)
		{
            AssertContractsIsAvailable();
            return contractsClient.HasPrivateKey(legalIdentityId);
        }

        public Task<LegalIdentity> GetLegalIdentity(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetLegalIdentityAsync(legalIdentityId);
        }

        public Task PetitionIdentity(string legalId, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionIdentityAsync(legalId, petitionId, purpose);
        }

        public Task SendPetitionIdentityResponse(string legalId, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionIdentityResponseAsync(legalId, petitionId, requestorFullJid, response);
        }

        public Task SendPetitionContractResponse(string contractId, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionContractResponseAsync(contractId, petitionId, requestorFullJid, response);
        }

        public Task SendPetitionSignatureResponse(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionSignatureResponseAsync(legalId, content, signature, petitionId, requestorFullJid, response);
        }

        public Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity identity, LegalIdentity reviewerLegalIdentity, byte[] peerSignature)
        {
            AssertContractsIsAvailable();
            return contractsClient.AddPeerReviewIDAttachment(identity, reviewerLegalIdentity, peerSignature);
        }

        public Task PetitionPeerReviewId(string legalId, LegalIdentity identity, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionPeerReviewIDAsync(legalId, identity, petitionId, purpose);
        }

        public Task<byte[]> Sign(byte[] data, SignWith signWith)
        {
            AssertContractsIsAvailable();
            return contractsClient.SignAsync(data, signWith);
        }

        public async Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null)
        {
            if(client == null)
            {
                AssertContractsIsAvailable();
                return await contractsClient.GetLegalIdentitiesAsync();
            }
            else
            {
                AssertContractsIsAvailable(false);
                using (ContractsClient cc = await ContractsClient.Create(client, this.tagProfile.LegalJid))
                {
                    return await cc.GetLegalIdentitiesAsync();
                }
            }
        }

        public bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature)
        {
            AssertContractsIsAvailable();
            return contractsClient.ValidateSignature(legalIdentity, data, signature);
        }

        public Task<LegalIdentity> ObsoleteLegalIdentity(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
        }

        public Task<LegalIdentity> CompromiseLegalIdentity(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.CompromisedLegalIdentityAsync(legalIdentityId);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void AssertContractsIsAvailable(bool checkClient = true)
        {
            if (!ContractsIsAvailable(checkClient))
            {
                throw new XmppFeatureNotSupportedException("ContractsClient is not initialized");
            }
        }

        private bool ContractsIsAvailable(bool checkClient = true)
        {
            bool clientIsOk = (checkClient && contractsClient != null) || !checkClient;

            return clientIsOk && !string.IsNullOrWhiteSpace(this.tagProfile.LegalJid);
        }

        private void AssertFileUploadIsAvailable()
        {
            if (!FileUploadIsAvailable())
            {
                throw new XmppFeatureNotSupportedException("FileUploadClient is not initialized");
            }
        }

        private bool FileUploadIsAvailable()
        {
            return fileUploadClient != null && this.tagProfile.FileUploadIsSupported;
        }

        #region Events

        public event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

        private void OnLegalIdentityChanged(LegalIdentityChangedEventArgs e)
        {
            LegalIdentityChanged?.Invoke(this, e);
        }

        public event EventHandler<LegalIdentityPetitionEventArgs> PetitionForIdentityReceived;

        private void OnPetitionForIdentityReceived(LegalIdentityPetitionEventArgs e)
        {
            PetitionForIdentityReceived?.Invoke(this, e);
        }

        public event EventHandler<SignaturePetitionEventArgs> PetitionForSignatureReceived;

        private void OnPetitionForSignatureReceived(SignaturePetitionEventArgs e)
        {
            PetitionForSignatureReceived?.Invoke(this, e);
        }

        public event EventHandler<LegalIdentityPetitionResponseEventArgs> PetitionedIdentityResponseReceived;

        private void OnPetitionedIdentityResponseReceived(LegalIdentityPetitionResponseEventArgs e)
        {
            PetitionedIdentityResponseReceived?.Invoke(this, e);
        }

        public event EventHandler<ContractPetitionEventArgs> PetitionForContractReceived;

        private void OnPetitionForContractReceived(ContractPetitionEventArgs e)
        {
            PetitionForContractReceived?.Invoke(this, e);
        }

        public event EventHandler<ContractPetitionResponseEventArgs> PetitionedContractResponseReceived;

        private void OnPetitionedContractResponseReceived(ContractPetitionResponseEventArgs e)
        {
            PetitionedContractResponseReceived?.Invoke(this, e);
        }

        public event EventHandler<SignaturePetitionEventArgs> PetitionForPeerReviewIdReceived;

        private void OnPetitionForPeerReviewIdReceived(SignaturePetitionEventArgs e)
        {
            PetitionForPeerReviewIdReceived?.Invoke(this, e);
        }

        public event EventHandler<SignaturePetitionResponseEventArgs> PetitionedPeerReviewIdResponseReceived;

        private void OnPetitionedPeerReviewIdResponseReceived(SignaturePetitionResponseEventArgs e)
        {
            PetitionedPeerReviewIdResponseReceived?.Invoke(this, e);
        }

        public event EventHandler<SignaturePetitionResponseEventArgs> SignaturePetitionResponseReceived;

        private void OnPetitionedSignatureResponseReceived(SignaturePetitionResponseEventArgs e)
        {
            SignaturePetitionResponseReceived?.Invoke(this, e);
        }

        #endregion

        #region Event Handlers

        private async Task ContractsClient_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            try
            {
                this.OnPetitionedContractResponseReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            try
            {
                this.OnPetitionForContractReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            try
            {
                this.OnPetitionedIdentityResponseReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            try
            {
                this.OnPetitionForIdentityReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionedPeerReviewIdResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                this.OnPetitionedPeerReviewIdResponseReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            try
            {
                this.OnPetitionForPeerReviewIdReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionedSignatureResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                this.OnPetitionedSignatureResponseReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            try
            {
                this.OnPetitionForSignatureReceived(e);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
            }
        }

        private async Task ContractsClient_IdentityUpdated(object sender, LegalIdentityEventArgs e)
        {
            if (this.tagProfile.LegalIdentity is null ||
                this.tagProfile.LegalIdentity.Id == e.Identity.Id ||
                this.tagProfile.LegalIdentity.Created < e.Identity.Created)
            {
                try
                {
                    OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex);
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
                }
            }
        }

        #endregion

        public bool IsOnline => this.contractsClient != null;

        public bool FileUploadIsSupported =>
            tagProfile.FileUploadIsSupported &&
            !(fileUploadClient is null) && fileUploadClient.HasSupport;
    }
}