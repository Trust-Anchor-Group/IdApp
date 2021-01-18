using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Navigation;
using XamarinApp.Views;
using XamarinApp.Views.Contracts;

namespace XamarinApp.Services
{
    internal sealed class ContractOrchestratorService : LoadableService, IContractOrchestratorService
    {
        private readonly ITagProfile tagProfile;
        private readonly IUiDispatcher uiDispatcher;
        private readonly INeuronService neuronService;
        private readonly INavigationService navigationService;
        private readonly ILogService logService;
        private readonly INetworkService networkService;

        public ContractOrchestratorService(
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService,
            INavigationService navigationService,
            ILogService logService,
            INetworkService networkService)
        {
            this.tagProfile = tagProfile;
            this.uiDispatcher = uiDispatcher;
            this.neuronService = neuronService;
            this.navigationService = navigationService;
            this.logService = logService;
            this.networkService = networkService;
        }

        public override Task Load(bool isResuming)
        {
            if (this.BeginLoad())
            {
                this.neuronService.Contracts.PetitionForPeerReviewIdReceived += Contracts_PetitionForPeerReviewIdReceived;
                this.neuronService.Contracts.PetitionForIdentityReceived += Contracts_PetitionForIdentityReceived;
                this.neuronService.Contracts.PetitionForSignatureReceived += Contracts_PetitionForSignatureReceived;
                this.neuronService.Contracts.PetitionedContractResponseReceived += Contracts_PetitionedNeuronContractResponseReceived;
                this.neuronService.Contracts.PetitionForContractReceived += Contracts_PetitionForNeuronContractReceived;
                this.neuronService.Contracts.PetitionedIdentityResponseReceived += Contracts_PetitionedIdentityResponseReceived;
                this.neuronService.Contracts.PetitionedPeerReviewIdResponseReceived += Contracts_PetitionedPeerReviewResponseReceived;
                this.EndLoad(true);
            }
            return Task.CompletedTask;
        }

        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                this.neuronService.Contracts.PetitionForPeerReviewIdReceived -= Contracts_PetitionForPeerReviewIdReceived;
                this.neuronService.Contracts.PetitionForIdentityReceived -= Contracts_PetitionForIdentityReceived;
                this.neuronService.Contracts.PetitionForSignatureReceived -= Contracts_PetitionForSignatureReceived;
                this.neuronService.Contracts.PetitionedContractResponseReceived -= Contracts_PetitionedNeuronContractResponseReceived;
                this.neuronService.Contracts.PetitionForContractReceived -= Contracts_PetitionForNeuronContractReceived;
                this.neuronService.Contracts.PetitionedIdentityResponseReceived -= Contracts_PetitionedIdentityResponseReceived;
                this.neuronService.Contracts.PetitionedPeerReviewIdResponseReceived -= Contracts_PetitionedPeerReviewResponseReceived;
                this.EndUnload();
            }

            return Task.CompletedTask;
        }

        #region Event Handlers

        private void Contracts_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
                {
                    if (this.tagProfile.IsCompleteOrWaitingForValidation())
                    {
                        await this.navigationService.PushAsync(new ViewIdentityPage(), new ViewIdentityNavigationArgs(e.RequestorIdentity, e));
                    }
                }
            );
        }

        private async void Contracts_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            LegalIdentity identity = null;

            if (e.RequestedIdentityId == this.tagProfile.LegalIdentity?.Id)
            {
                identity = this.tagProfile.LegalIdentity;
            }
            else
            {
                (bool succeeded, LegalIdentity li) = await this.networkService.Request(this.neuronService.Contracts.GetLegalIdentityAsync, e.RequestedIdentityId);
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
                await this.networkService.Request(this.neuronService.Contracts.SendPetitionIdentityResponseAsync, e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (this.tagProfile.IsCompleteOrWaitingForValidation())
                    {
                        await this.navigationService.PushAsync(new PetitionIdentityPage(), new PetitionIdentityNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
                    }
                });
            }
        }

        private async void Contracts_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
        {
            // Reject all signature requests by default
            await this.networkService.Request(this.neuronService.Contracts.SendPetitionSignatureResponseAsync, e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false);
        }

        private void Contracts_PetitionedNeuronContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (!e.Response || e.RequestedContract is null)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.Message, AppResources.PetitionToViewContractWasDenied, AppResources.Ok);
                }
                else
                {
                    await this.navigationService.PushAsync(new ViewContractPage(), new ViewContractNavigationArgs(e.RequestedContract, false));
                }
            });
        }

        private async void Contracts_PetitionForNeuronContractReceived(object sender, ContractPetitionEventArgs e)
        {
            (bool succeeded, Contract contract) = await this.networkService.Request(this.neuronService.Contracts.GetContractAsync, e.RequestedContractId);

            if (!succeeded)
                return;

            if (contract.State == ContractState.Deleted ||
                contract.State == ContractState.Rejected)
            {
                await this.networkService.Request(this.neuronService.Contracts.SendPetitionContractResponseAsync, e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false);
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.navigationService.PushAsync(new PetitionContractPage(), new PetitionContractNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, contract, e.PetitionId, e.Purpose));
                });
            }
        }

        private async void Contracts_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            if (!e.Response || e.RequestedIdentity is null)
            {
                await this.uiDispatcher.DisplayAlert(AppResources.Message, AppResources.PetitionToViewLegalIdentityWasDenied, AppResources.Ok);
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.navigationService.PushAsync(new ViewIdentityPage(), new ViewIdentityNavigationArgs(e.RequestedIdentity, null));
                });
            }
        }

        private async void Contracts_PetitionedPeerReviewResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
        {
            try
            {
                if (!e.Response)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasRejected, AppResources.Ok);
                }
                else
                {
                    StringBuilder xml = new StringBuilder();
                    tagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
                    byte[] data = Encoding.UTF8.GetBytes(xml.ToString());
                    bool? result;

                    try
                    {
                        result = this.neuronService.Contracts.ValidateSignature(e.RequestedIdentity, data, e.Signature);
                    }
                    catch (Exception ex)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
                        return;
                    }

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (!result.HasValue || !result.Value)
                        {
                            await this.uiDispatcher.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError, AppResources.Ok);
                        }
                        else
                        {
                            (bool succeeded, LegalIdentity legalIdentity) = await this.networkService.Request(this.neuronService.Contracts.AddPeerReviewIdAttachment, tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature);
                            if (succeeded)
                            {
                                await this.uiDispatcher.DisplayAlert(AppResources.PeerReviewAccepted, AppResources.APeerReviewYouhaveRequestedHasBeenAccepted, AppResources.Ok);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }

        }

        #endregion

        public async Task OpenLegalIdentity(string legalId, string purpose)
        {
            try
            {
                LegalIdentity identity = await this.neuronService.Contracts.GetLegalIdentityAsync(legalId);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await this.navigationService.PushAsync(new ViewIdentityPage(), new ViewIdentityNavigationArgs(identity, null));
                });
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, 
                    new KeyValuePair<string, string>("Class", nameof(ContractOrchestratorService)),
                    new KeyValuePair<string, string>("Method", nameof(OpenLegalIdentity)));

                bool succeeded = await this.networkService.Request(this.neuronService.Contracts.PetitionIdentityAsync, legalId, Guid.NewGuid().ToString(), purpose);
                if (succeeded)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheOwner);
                }
            }
        }

        public async Task OpenContract(string contractId, string purpose)
        {
            try
            {
                Contract contract = await this.neuronService.Contracts.GetContractAsync(contractId);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
                        await this.navigationService.PushAsync(new NewContractPage(), new NewContractNavigationArgs(contract));
                    else
                        await this.navigationService.PushAsync(new ViewContractPage(), new ViewContractNavigationArgs(contract, false));
                });
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex,
                    new KeyValuePair<string, string>("Class", nameof(ContractOrchestratorService)),
                    new KeyValuePair<string, string>("Method", nameof(OpenContract)));

                bool succeeded = await this.networkService.Request(this.neuronService.Contracts.PetitionContractAsync, contractId, Guid.NewGuid().ToString(), purpose);
                if (succeeded)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheContract);
                }
            }
        }
    }
}