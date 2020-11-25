using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Runtime.Temporary;

namespace XamarinApp.Services
{
    internal sealed class ContractsService : IContractsService
    {
        private static readonly TimeSpan FileUploadTimeout = TimeSpan.FromSeconds(30);

        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly IMessageService messageService;
        private ContractsClient contractsClient;
        private HttpFileUploadClient fileUploadClient;

        public ContractsService(TagProfile tagProfile, INeuronService neuronService, IMessageService messageService)
        {
            this.tagProfile = tagProfile;
            this.neuronService = neuronService;
            this.messageService = messageService;
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        public void Dispose()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
        }

        private async Task CreateContractsClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
            {
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
            }
        }

        private void DestroyContractsClient()
        {
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
            }
        }

        private async Task CreateFileUploadClient()
        {
            if (!string.IsNullOrEmpty(this.tagProfile.HttpFileUploadJid) && this.tagProfile.HttpFileUploadMaxSize.HasValue)
            {
                this.fileUploadClient = await this.neuronService.CreateFileUploadClientAsync();
            }
        }

        private void DestroyFileUploadClient()
        {
            fileUploadClient?.Dispose();
            fileUploadClient = null;
        }

        private async void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (this.neuronService.State == XmppState.Connected)
            {
                if (this.contractsClient == null)
                {
                    await this.CreateContractsClient();
                }
                if (this.fileUploadClient == null)
                {
                    await this.CreateFileUploadClient();
                }
            }
            else
            {
                this.DestroyFileUploadClient();
                this.DestroyContractsClient();
            }
        }

        public Task PetitionContractAsync(string contractId, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionContractAsync(contractId, petitionId, purpose);
        }

        public Task<Contract> GetContractAsync(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetContractAsync(contractId);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetAttachmentAsync(url);
        }

        public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url, TimeSpan timeout)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetAttachmentAsync(url, (int)timeout.TotalMilliseconds);
        }

        public Task<Contract> CreateContractAsync(
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

        public Task<Contract> DeleteContractAsync(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.DeleteContractAsync(contractId);
        }

        public Task<string[]> GetCreatedContractsAsync()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetCreatedContractsAsync();
        }

        public Task<string[]> GetSignedContractsAsync()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetSignedContractsAsync();
        }

        public Task<Contract> SignContractAsync(Contract contract, string role, bool transferable)
        {
            AssertContractsIsAvailable();
            return contractsClient.SignContractAsync(contract, role, transferable);
        }

        public Task<Contract> ObsoleteContractAsync(string contractId)
        {
            AssertContractsIsAvailable();
            return contractsClient.ObsoleteContractAsync(contractId);
        }

        public async Task<LegalIdentity> AddLegalIdentityAsync(List<Property> properties, params LegalIdentityAttachment[] attachments)
        {
            AssertContractsIsAvailable();
            AssertFileUploadIsAvailable();

            LegalIdentity identity = await contractsClient.ApplyAsync(properties.ToArray());

            foreach (var a in attachments)
            {
                HttpFileUploadEventArgs e2 = await fileUploadClient.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
                if (!e2.Ok)
                {
                    throw new Exception(e2.ErrorText);
                }

                await e2.PUT(a.Data, a.ContentType, (int)FileUploadTimeout.TotalMilliseconds);

                byte[] signature = await contractsClient.SignAsync(a.Data);

                identity = await contractsClient.AddLegalIdAttachmentAsync(identity.Id, e2.GetUrl, signature);
            }

            return identity;
        }

        public Task<LegalIdentity> GetLegalIdentityAsync(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.GetLegalIdentityAsync(legalIdentityId);
        }

        public Task PetitionIdentityAsync(string legalId, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionIdentityAsync(legalId, petitionId, purpose);
        }

        public Task SendPetitionIdentityResponseAsync(string legalId, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionIdentityResponseAsync(legalId, petitionId, requestorFullJid, response);
        }

        public Task SendPetitionContractResponseAsync(string contractId, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionContractResponseAsync(contractId, petitionId, requestorFullJid, response);
        }

        public Task SendPetitionSignatureResponseAsync(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionSignatureResponseAsync(legalId, content, signature, petitionId, requestorFullJid, response);
        }

        public Task PetitionPeerReviewIdAsync(string legalId, LegalIdentity identity, string petitionId, string purpose)
        {
            AssertContractsIsAvailable();
            return contractsClient.PetitionPeerReviewIDAsync(legalId, identity, petitionId, purpose);
        }

        public Task<byte[]> SignAsync(byte[] data)
        {
            AssertContractsIsAvailable();
            return contractsClient.SignAsync(data);
        }

        public Task<LegalIdentity[]> GetLegalIdentitiesAsync()
        {
            AssertContractsIsAvailable();
            return contractsClient.GetLegalIdentitiesAsync();
        }

        public Task<LegalIdentity> ObsoleteLegalIdentityAsync(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
        }

        public Task<LegalIdentity> CompromisedLegalIdentityAsync(string legalIdentityId)
        {
            AssertContractsIsAvailable();
            return contractsClient.CompromisedLegalIdentityAsync(legalIdentityId);
        }

        private void AssertContractsIsAvailable()
        {
            if (contractsClient == null || string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
            {
                throw new XmppFeatureNotSupportedException("Contracts is not supported");
            }
        }

        private void AssertFileUploadIsAvailable()
        {
            if (fileUploadClient == null || !this.tagProfile.FileUploadIsSupported)
            {
                throw new XmppFeatureNotSupportedException("FileUpload is not supported");
            }
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

        #endregion

        #region Event Handlers

        private Task ContractsClient_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            this.OnPetitionedContractResponseReceived(e);
            return Task.CompletedTask;
        }

        private async Task ContractsClient_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            try
            {
                Contract contract = await contractsClient.GetContractAsync(e.RequestedContractId);

                if (contract.State == ContractState.Deleted ||
                    contract.State == ContractState.Rejected)
                {
                    await contractsClient.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
                }
                else
                {
                    this.OnPetitionForContractReceived(e);
                }
            }
            catch (Exception)
            {
                await contractsClient.PetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
            }
        }

        private Task ContractsClient_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            this.OnPetitionedIdentityResponseReceived(e);
            return Task.CompletedTask;
        }

        private async Task ContractsClient_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            try
            {
                LegalIdentity identity;

                if (e.RequestedIdentityId == this.tagProfile.LegalIdentity.Id)
                    identity = this.tagProfile.LegalIdentity;
                else
                    identity = await contractsClient.GetLegalIdentityAsync(e.RequestedIdentityId);

                if (identity.State == IdentityState.Compromised ||
                    identity.State == IdentityState.Rejected)
                {
                    await contractsClient.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
                }
                else
                {
                    this.OnPetitionForIdentityReceived(e);
                }
            }
            catch (Exception)
            {
                await contractsClient.PetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
            }
        }

        private async Task ContractsClient_PetitionedPeerReviewIdResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                if (!e.Response)
                {
                    await this.messageService.DisplayAlert(AppResources.PeerReviewRejected, "A peer you requested to review your application, has rejected to approve it.", AppResources.Ok);
                }
                else
                {
                    StringBuilder xml = new StringBuilder();
                    tagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
                    byte[] data = Encoding.UTF8.GetBytes(xml.ToString());

                    bool? result = contractsClient.ValidateSignature(e.RequestedIdentity, data, e.Signature);
                    if (!result.HasValue || !result.Value)
                    {
                        await this.messageService.DisplayAlert(AppResources.PeerReviewRejected, "A peer review you requested has been rejected, due to a signature error.", AppResources.Ok);
                    }
                    else
                    {
                        await contractsClient.AddPeerReviewIDAttachment(tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature);
                        await this.messageService.DisplayAlert(AppResources.PeerReviewAccepted, "A peer review you requested has been accepted.", AppResources.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }
        }

        private Task ContractsClient_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            this.OnPetitionForPeerReviewIdReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionedSignatureResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            // Reject all signature requests by default
            return contractsClient.PetitionSignatureResponseAsync(e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false);
        }

        private Task ContractsClient_IdentityUpdated(object sender, LegalIdentityEventArgs e)
        {
            if (this.tagProfile.LegalIdentity is null ||
                this.tagProfile.LegalIdentity.Id == e.Identity.Id ||
                this.tagProfile.LegalIdentity.Created < e.Identity.Created)
            {
                OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
            }

            return Task.CompletedTask;
        }

        #endregion

        public bool FileUploadIsSupported =>
            tagProfile.FileUploadIsSupported &&
            !(fileUploadClient is null) && fileUploadClient.HasSupport;
    }
}