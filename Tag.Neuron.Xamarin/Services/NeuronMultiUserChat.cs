﻿using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.MUC;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal sealed class NeuronMultiUserChat : INeuronMultiUserChat
    {
        private readonly ITagProfile tagProfile;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IInternalNeuronService neuronService;
        private readonly ILogService logService;
        private MultiUserChatClient mucClient;

        internal NeuronMultiUserChat(ITagProfile tagProfile, IUiDispatcher uiDispatcher, IInternalNeuronService neuronService, ILogService logService)
        {
            this.tagProfile = tagProfile;
            this.uiDispatcher = uiDispatcher;
            this.neuronService = neuronService;
            this.logService = logService;
        }

        internal void CreateClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.MucJid))
            {
                this.mucClient = this.neuronService.CreateMultiUserChatClient();
                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(GetState(), false));
            }
        }

        internal void DestroyClient()
        {
            if (!(this.mucClient is null))
            {
                this.mucClient.Dispose();
                this.mucClient = null;
                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(GetState(), false));
            }
        }

        public bool IsOnline => !(this.mucClient is null);

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        private XmppState GetState()
        {
            return this.mucClient.Client.State;
        }

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            this.ConnectionStateChanged?.Invoke(this, e);
        }
    }
}