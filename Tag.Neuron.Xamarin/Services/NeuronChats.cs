using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.MUC;

namespace Tag.Neuron.Xamarin.Services
{
    internal sealed class NeuronChats : INeuronChats
    {
        private readonly ITagProfile tagProfile;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IInternalNeuronService neuronService;
        private readonly ILogService logService;
        private MultiUserChatClient chatClient;

        internal NeuronChats(ITagProfile tagProfile, IUiDispatcher uiDispatcher, IInternalNeuronService neuronService, ILogService logService)
        {
            this.tagProfile = tagProfile;
            this.uiDispatcher = uiDispatcher;
            this.neuronService = neuronService;
            this.logService = logService;
        }

        public void Dispose()
        {
            this.DestroyClient();
        }

        internal async Task CreateClient()
        {
            if (!string.IsNullOrWhiteSpace(this.tagProfile.LegalJid))
            {
                this.chatClient = await this.neuronService.CreateMultiUserChatClientAsync();
                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(GetState()));
            }
        }

        internal void DestroyClient()
        {
            if (this.chatClient != null)
            {
                this.chatClient.Dispose();
                this.chatClient = null;
                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(GetState()));
            }
        }

        public bool IsOnline => this.chatClient != null;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        private XmppState GetState()
        {
            return this.chatClient != null ? XmppState.Connected : XmppState.Offline;
        }

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            this.ConnectionStateChanged?.Invoke(this, e);
        }
    }
}