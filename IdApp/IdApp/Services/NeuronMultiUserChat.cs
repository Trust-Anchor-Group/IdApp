using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.MUC;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    [Singleton]
    internal sealed class NeuronMultiUserChat : INeuronMultiUserChat
    {
        private readonly INeuronService neuronService;

        internal NeuronMultiUserChat(INeuronService neuronService)
        {
            this.neuronService = neuronService;
        }

        public bool IsOnline => !(this.neuronService.IsOnline);

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
    }
}