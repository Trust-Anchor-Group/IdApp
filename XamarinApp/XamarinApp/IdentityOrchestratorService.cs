using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;
using XamarinApp.Views;
using XamarinApp.Views.Contracts;

namespace XamarinApp
{
    internal sealed class IdentityOrchestratorService : LoadableService, IIdentityOrchestratorService
    {
        private readonly TagProfile tagProfile;
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;

        public IdentityOrchestratorService(TagProfile tagProfile, IContractsService contractsService, INavigationService navigationService)
        {
            this.tagProfile = tagProfile;
            this.contractsService = contractsService;
            this.navigationService = navigationService;
        }

        public override Task Load()
        {
            if (this.BeginLoad())
            {
                this.contractsService.PetitionForPeerReviewIdReceived += ContractsService_PetitionForPeerReviewIdReceived;
                this.contractsService.PetitionForIdentityReceived += ContractsService_PetitionForIdentityReceived;
                this.contractsService.PetitionForSignatureReceived += ContractsService_PetitionForSignatureReceived;
                this.contractsService.PetitionedContractResponseReceived += ContractsService_PetitionedContractResponseReceived;
                this.contractsService.PetitionForContractReceived += ContractsService_PetitionForContractReceived;
                this.contractsService.PetitionedIdentityResponseReceived += ContractsService_PetitionedIdentityResponseReceived;
                this.contractsService.PetitionedPeerReviewIdResponseReceived += ContractsService_PetitionedPeerReviewResponseReceived;
                this.EndLoad(true);
            }
            return Task.CompletedTask;
        }

        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                this.contractsService.PetitionForPeerReviewIdReceived -= ContractsService_PetitionForPeerReviewIdReceived;
                this.contractsService.PetitionForIdentityReceived -= ContractsService_PetitionForIdentityReceived;
                this.contractsService.PetitionForSignatureReceived -= ContractsService_PetitionForSignatureReceived;
                this.contractsService.PetitionedContractResponseReceived -= ContractsService_PetitionedContractResponseReceived;
                this.contractsService.PetitionForContractReceived -= ContractsService_PetitionForContractReceived;
                this.contractsService.PetitionedIdentityResponseReceived -= ContractsService_PetitionedIdentityResponseReceived;
                this.contractsService.PetitionedPeerReviewIdResponseReceived -= ContractsService_PetitionedPeerReviewResponseReceived;
                this.EndUnload();
            }

            return Task.CompletedTask;
        }

        #region Event Handlers

        private async void ContractsService_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            if (this.tagProfile.IsCompleteOrWaitingForValidation())
            {
                await this.navigationService.PushAsync(new ViewIdentityPage(e.RequestorIdentity, e));
            }
        }

        private async void ContractsService_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            LegalIdentity identity;

            if (e.RequestedIdentityId == this.tagProfile.LegalIdentity.Id)
                identity = this.tagProfile.LegalIdentity;
            else
                identity = await this.contractsService.GetLegalIdentityAsync(e.RequestedIdentityId);

            if (identity.State == IdentityState.Compromised ||
                identity.State == IdentityState.Rejected)
            {
                await this.contractsService.SendPetitionIdentityResponseAsync(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
            }
            else
            {
                if (this.tagProfile.IsCompleteOrWaitingForValidation())
                {
                    await this.navigationService.PushAsync(new PetitionIdentityPage(e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
                }
            }
        }

        private async void ContractsService_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            // Reject all signature requests by default
            await this.contractsService.SendPetitionSignatureResponseAsync(e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false);
        }

        private async void ContractsService_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            if (!e.Response || e.RequestedContract is null)
            {
                await this.navigationService.DisplayAlert(AppResources.Message, "Petition to view contract was denied.", AppResources.Ok);
            }
            else
            {
                await this.navigationService.PushAsync(new ViewContractPage(e.RequestedContract, false));
            }
        }

        private async void ContractsService_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            Contract contract = await this.contractsService.GetContractAsync(e.RequestedContractId);

            if (contract.State == ContractState.Deleted ||
                contract.State == ContractState.Rejected)
            {
                await this.contractsService.SendPetitionContractResponseAsync(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
            }
            else
            {
                await this.navigationService.PushAsync(new PetitionContractPage(e.RequestorIdentity, e.RequestorFullJid, contract, e.PetitionId, e.Purpose));
            }
        }

        private async void ContractsService_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            if (!e.Response || e.RequestedIdentity is null)
            {
                await this.navigationService.DisplayAlert(AppResources.Message, "Petition to view legal identity was denied.", AppResources.Ok);
            }
            else
            {
                await this.navigationService.PushAsync(new ViewIdentityPage(e.RequestedIdentity));
            }
        }

        private async void ContractsService_PetitionedPeerReviewResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                if (!e.Response)
                {
                    await this.navigationService.DisplayAlert(AppResources.PeerReviewRejected, "A peer you requested to review your application, has rejected to approve it.", AppResources.Ok);
                }
                else
                {
                    StringBuilder xml = new StringBuilder();
                    tagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
                    byte[] data = Encoding.UTF8.GetBytes(xml.ToString());

                    bool? result = this.contractsService.ValidateSignature(e.RequestedIdentity, data, e.Signature);
                    if (!result.HasValue || !result.Value)
                    {
                        await this.navigationService.DisplayAlert(AppResources.PeerReviewRejected, "A peer review you requested has been rejected, due to a signature error.", AppResources.Ok);
                    }
                    else
                    {
                        await this.contractsService.AddPeerReviewIdAttachment(tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature);
                        await this.navigationService.DisplayAlert(AppResources.PeerReviewAccepted, "A peer review you requested has been accepted.", AppResources.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }

        }

        #endregion

        public async Task OpenLegalIdentity(string legalId, string purpose)
        {
            try
            {
                LegalIdentity identity = await this.contractsService.GetLegalIdentityAsync(legalId);
                await this.navigationService.PushAsync(new ViewIdentityPage(identity));
            }
            catch (Exception)
            {
                await this.contractsService.PetitionIdentityAsync(legalId, Guid.NewGuid().ToString(), purpose);

                await this.navigationService.DisplayAlert("Petition Sent", "A petition has been sent to the owner of the identity. " +
                                                                           "If the owner accepts the petition, the identity information will be displayed on the screen.");
            }
        }

        public async Task OpenContract(string contractId, string purpose)
        {
            try
            {
                Contract contract = await this.contractsService.GetContractAsync(contractId);

                if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
                    await this.navigationService.PushAsync(new NewContractPage(contract));
                else
                    await this.navigationService.PushAsync(new ViewContractPage(contract, false));
            }
            catch (Exception)
            {
                await this.contractsService.PetitionContractAsync(contractId, Guid.NewGuid().ToString(), purpose);

                await this.navigationService.DisplayAlert("Petition Sent", "A petition has been sent to the parts of the contract. " +
                                                                           "If any of the parts accepts the petition, the contract information will be displayed on the screen.");
            }
        }
    }
}