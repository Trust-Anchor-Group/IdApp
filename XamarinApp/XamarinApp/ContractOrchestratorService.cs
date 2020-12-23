using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Services;
using XamarinApp.Views;
using XamarinApp.Views.Contracts;

namespace XamarinApp
{
    internal sealed class ContractOrchestratorService : LoadableService, IContractOrchestratorService
    {
        private readonly TagProfile tagProfile;
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;
        private readonly ILogService logService;
        private readonly INetworkService networkService;

        public ContractOrchestratorService(
            TagProfile tagProfile, 
            IContractsService contractsService, 
            INavigationService navigationService,
            ILogService logService,
            INetworkService networkService)
        {
            this.tagProfile = tagProfile;
            this.contractsService = contractsService;
            this.navigationService = navigationService;
            this.logService = logService;
            this.networkService = networkService;
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

        private void ContractsService_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
                {
                    if (this.tagProfile.IsCompleteOrWaitingForValidation())
                    {
                        await this.navigationService.PushAsync(new ViewIdentityPage(e.RequestorIdentity, e));
                    }
                }
            );
        }

        private async void ContractsService_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            LegalIdentity identity = null;

            if (e.RequestedIdentityId == this.tagProfile.LegalIdentity?.Id)
            {
                identity = this.tagProfile.LegalIdentity;
            }
            else
            {
                (bool succeeded, LegalIdentity li) = await this.networkService.Request(this.navigationService, this.contractsService.GetLegalIdentityAsync, e.RequestedIdentityId);
                if (succeeded)
                {
                    identity = li;
                }
                else
                {
                    return;
                }
            }

            if (identity.State == IdentityState.Compromised ||
                identity.State == IdentityState.Rejected)
            {
                await this.networkService.Request(this.navigationService, this.contractsService.SendPetitionIdentityResponseAsync, e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (this.tagProfile.IsCompleteOrWaitingForValidation())
                    {
                        await this.navigationService.PushAsync(new PetitionIdentityPage(e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
                    }
                });
            }
        }

        private async void ContractsService_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            // Reject all signature requests by default
            await this.networkService.Request(this.navigationService, this.contractsService.SendPetitionSignatureResponseAsync, e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false);
        }

        private void ContractsService_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (!e.Response || e.RequestedContract is null)
                {
                    await this.navigationService.DisplayAlert(AppResources.Message, AppResources.PetitionToViewContractWasDenied, AppResources.Ok);
                }
                else
                {
                    await this.navigationService.PushAsync(new ViewContractPage(e.RequestedContract, false));
                }
            });
        }

        private async void ContractsService_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            (bool succeeded, Contract contract) = await this.networkService.Request(this.navigationService, this.contractsService.GetContractAsync, e.RequestedContractId);

            if (!succeeded)
                return;

            if (contract.State == ContractState.Deleted ||
                contract.State == ContractState.Rejected)
            {
                await this.networkService.Request(this.navigationService, this.contractsService.SendPetitionContractResponseAsync, e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.navigationService.PushAsync(new PetitionContractPage(e.RequestorIdentity, e.RequestorFullJid, contract, e.PetitionId, e.Purpose));
                });
            }
        }

        private async void ContractsService_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            if (!e.Response || e.RequestedIdentity is null)
            {
                await this.navigationService.DisplayAlert(AppResources.Message, AppResources.PetitionToViewLegalIdentityWasDenied, AppResources.Ok);
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.navigationService.PushAsync(new ViewIdentityPage(e.RequestedIdentity));
                });
            }
        }

        private async void ContractsService_PetitionedPeerReviewResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                if (!e.Response)
                {
                    await this.navigationService.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasRejected, AppResources.Ok);
                }
                else
                {
                    StringBuilder xml = new StringBuilder();
                    tagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
                    byte[] data = Encoding.UTF8.GetBytes(xml.ToString());
                    bool? result;

                    try
                    {
                        result = this.contractsService.ValidateSignature(e.RequestedIdentity, data, e.Signature);
                    }
                    catch (Exception ex)
                    {
                        await this.navigationService.DisplayAlert(AppResources.ErrorTitle, ex.Message);
                        return;
                    }

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (!result.HasValue || !result.Value)
                        {
                            await this.navigationService.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError, AppResources.Ok);
                        }
                        else
                        {
                            (bool succeeded, LegalIdentity legalIdentity) = await this.networkService.Request(this.navigationService, this.contractsService.AddPeerReviewIdAttachment, tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature);
                            if (succeeded)
                            {
                                await this.navigationService.DisplayAlert(AppResources.PeerReviewAccepted, AppResources.APeerReviewYouhaveRequestedHasBeenAccepted, AppResources.Ok);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.navigationService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }

        }

        #endregion

        public async Task OpenLegalIdentity(string legalId, string purpose)
        {
            try
            {
                LegalIdentity identity = await this.contractsService.GetLegalIdentityAsync(legalId);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.navigationService.PushAsync(new ViewIdentityPage(identity));
                });
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, 
                    new KeyValuePair<string, string>("Class", nameof(ContractOrchestratorService)),
                    new KeyValuePair<string, string>("Method", nameof(OpenLegalIdentity)));

                bool succeeded = await this.networkService.Request(this.navigationService, this.contractsService.PetitionIdentityAsync, legalId, Guid.NewGuid().ToString(), purpose);
                if (succeeded)
                {
                    await this.navigationService.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheOwner);
                }
            }
        }

        public async Task OpenContract(string contractId, string purpose)
        {
            try
            {
                Contract contract = await this.contractsService.GetContractAsync(contractId);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
                        await this.navigationService.PushAsync(new NewContractPage(contract));
                    else
                        await this.navigationService.PushAsync(new ViewContractPage(contract, false));
                });
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex,
                    new KeyValuePair<string, string>("Class", nameof(ContractOrchestratorService)),
                    new KeyValuePair<string, string>("Method", nameof(OpenContract)));

                bool succeeded = await this.networkService.Request(this.navigationService, this.contractsService.PetitionContractAsync, contractId, Guid.NewGuid().ToString(), purpose);
                if (succeeded)
                {
                    await this.navigationService.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheContract);
                }
            }
        }
    }
}