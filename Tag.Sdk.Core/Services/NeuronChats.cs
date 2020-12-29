using System.Threading.Tasks;
using Waher.Networking.XMPP.MUC;

namespace Tag.Sdk.Core.Services
{
    internal sealed class NeuronChats : INeuronChats
    {
        private readonly TagProfile tagProfile;
        private readonly IDispatcher dispatcher;
        private readonly IInternalNeuronService neuronService;
        private readonly ILogService logService;
        private MultiUserChatClient chatClient;

        internal NeuronChats(TagProfile tagProfile, IDispatcher dispatcher, IInternalNeuronService neuronService, ILogService logService)
        {
            this.tagProfile = tagProfile;
            this.dispatcher = dispatcher;
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
            }
        }

        internal void DestroyClient()
        {
            if (this.chatClient != null)
            {
                this.chatClient.Dispose();
                this.chatClient = null;
            }
        }

        public bool IsOnline => this.chatClient != null;
    }
}