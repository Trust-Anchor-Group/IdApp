using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Views.Registration;

namespace XamarinApp.Services
{
    internal sealed class NavigationOrchestratorService : LoadableService, INavigationOrchestratorService
    {
        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly INetworkService networkService;
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;

        public NavigationOrchestratorService(TagProfile tagProfile, INeuronService neuronService, INetworkService networkService, IContractsService contractsService, INavigationService navigationService)
        {
            this.tagProfile = tagProfile;
            this.neuronService = neuronService;
            this.networkService = networkService;
            this.contractsService = contractsService;
            this.navigationService = navigationService;
        }

        public override Task Load()
        {
            if (this.BeginLoad())
            {
                this.tagProfile.Changed += TagProfile_Changed;
                this.neuronService.Loaded += NeuronService_Loaded;
                this.EndLoad(true);
            }
            return Task.CompletedTask;
        }

        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                this.tagProfile.Changed -= TagProfile_Changed;
                this.neuronService.Loaded -= NeuronService_Loaded;
                this.EndUnload();
            }

            return Task.CompletedTask;
        }

        private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
        {

        }

        private async void NeuronService_Loaded(object sender, LoadedEventArgs e)
        {
            // TODO: wait for ContractsService to be initialized also. Move this to ContractOrchestratorService?

            //if (this.tagProfile.LegalIdentity != null && this.tagProfile.IsCompleteOrWaitingForValidation())
            //{
            //    string legalId = this.tagProfile.LegalIdentity.Id;
            //    (bool succeeded, LegalIdentity identity) = await this.networkService.Request(this.navigationService, this.contractsService.GetLegalIdentityAsync, legalId, displayAlert: false);
            //    if (succeeded)
            //    {
            //        bool gotoRegistrationPage = false;
            //        if (identity.State == IdentityState.Compromised)
            //        {
            //            this.tagProfile.CompromiseLegalIdentity(identity);
            //            gotoRegistrationPage = true;
            //        }
            //        else if (identity.State == IdentityState.Obsoleted)
            //        {
            //            this.tagProfile.RevokeLegalIdentity(identity);
            //            gotoRegistrationPage = true;
            //        }
            //        else
            //        {
            //            this.tagProfile.SetLegalIdentity(identity);
            //        }
            //        if (gotoRegistrationPage)
            //        {
            //            await this.navigationService.ReplaceAsync(new RegistrationPage());
            //        }
            //    }
            //}
        }
    }
}