﻿using System;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Views.Registration;

namespace XamarinApp.Services
{
    internal class NavigationOrchestratorService : LoadableService, INavigationOrchestratorService
    {
        private readonly ITagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly INetworkService networkService;
        private readonly INavigationService navigationService;

        public NavigationOrchestratorService(ITagProfile tagProfile, INeuronService neuronService, INetworkService networkService, INavigationService navigationService)
        {
            this.tagProfile = tagProfile;
            this.neuronService = neuronService;
            this.networkService = networkService;
            this.navigationService = navigationService;
        }

        public override Task Load(bool isResuming)
        {
            if (this.BeginLoad())
            {
                this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
                this.EndLoad(true);
            }
            return Task.CompletedTask;
        }

        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
                this.EndUnload();
            }

            return Task.CompletedTask;
        }

        private async void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == XmppState.Connected)
            {
                await Task.Delay(Constants.Timeouts.XmppInit);
                if (this.tagProfile.LegalIdentity != null && this.tagProfile.IsCompleteOrWaitingForValidation())
                {
                    DownloadLegalIdentityInternal(this.tagProfile.LegalIdentity.Id);
                }
            }
        }

        protected virtual void DownloadLegalIdentityInternal(string legalId)
        {
            // Run asynchronously so we're not blocking startup UI.
            _ = DownloadLegalIdentity(this.tagProfile.LegalIdentity.Id);
        }

        private async Task DownloadLegalIdentity(string legalId)
        {
            bool isConnected = await this.neuronService.WaitForConnectedState(Constants.Timeouts.XmppConnect);

            if (!isConnected)
                return;

            (bool succeeded, LegalIdentity identity) = await this.networkService.Request(this.neuronService.Contracts.GetLegalIdentityAsync, legalId, displayAlert: false);
            if (succeeded)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    bool gotoRegistrationPage = false;
                    if (identity.State == IdentityState.Compromised)
                    {
                        this.tagProfile.CompromiseLegalIdentity(identity);
                        gotoRegistrationPage = true;
                    }
                    else if (identity.State == IdentityState.Obsoleted)
                    {
                        this.tagProfile.RevokeLegalIdentity(identity);
                        gotoRegistrationPage = true;
                    }
                    else
                    {
                        this.tagProfile.SetLegalIdentity(identity);
                    }
                    if (gotoRegistrationPage)
                    {
                        await this.navigationService.ReplaceAsync(new RegistrationPage());
                    }
                });
            }

        }
    }
}