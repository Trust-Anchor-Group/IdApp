using System;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Neuron
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