﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Runtime.Temporary;
using XamarinApp.Models;

namespace XamarinApp.Services
{
    internal sealed class ContractsService : IContractsService
    {
        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly ILogService logService;
        private ContractsClient contractsClient;
        private HttpFileUploadClient fileUploadClient;

        public ContractsService(TagProfile tagProfile, INeuronService neuronService, ILogService logService)
        {
            this.tagProfile = tagProfile;
            this.neuronService = neuronService;
            this.logService = logService;
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
                this.contractsClient = null;
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

        private async void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == XmppState.Connected)
            {
                if (this.contractsClient == null)
                {
                    try
                    {
                        await this.CreateContractsClient();
                    }
                    catch (Exception ex)
                    {
                        this.logService.LogException(ex);
                        this.contractsClient = null;
                    }
                }
                if (this.fileUploadClient == null)
                {
                    try
                    {
                        await this.CreateFileUploadClient();
                    }
                    catch (Exception ex)
                    {
                        this.logService.LogException(ex);
                        this.fileUploadClient = null;
                    }
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

        public async Task<LegalIdentity> AddLegalIdentityAsync(RegisterIdentityModel model, params LegalIdentityAttachment[] attachments)
        {
            AssertContractsIsAvailable();
            AssertFileUploadIsAvailable();

            LegalIdentity identity = await contractsClient.ApplyAsync(model.ToProperties(this.neuronService));

            foreach (var a in attachments)
            {
                HttpFileUploadEventArgs e2 = await fileUploadClient.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
                if (!e2.Ok)
                {
                    throw new Exception(e2.ErrorText);
                }

                await e2.PUT(a.Data, a.ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

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

        public Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity identity, LegalIdentity reviewerLegalIdentity, byte[] peerSignature)
        {
            AssertContractsIsAvailable();
            return contractsClient.AddPeerReviewIDAttachment(identity, reviewerLegalIdentity, peerSignature);
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

        public async Task<LegalIdentity[]> GetLegalIdentitiesAsync(XmppClient client = null)
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

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void AssertContractsIsAvailable(bool checkClient = true)
        {
            if ((checkClient && contractsClient == null) || string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
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

        private Task ContractsClient_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            this.OnPetitionedContractResponseReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            this.OnPetitionForContractReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            this.OnPetitionedIdentityResponseReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            this.OnPetitionForIdentityReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionedPeerReviewIdResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            this.OnPetitionedPeerReviewIdResponseReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            this.OnPetitionForPeerReviewIdReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionedSignatureResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            this.OnPetitionedSignatureResponseReceived(e);
            return Task.CompletedTask;
        }

        private Task ContractsClient_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            this.OnPetitionForSignatureReceived(e);
            return Task.CompletedTask;
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