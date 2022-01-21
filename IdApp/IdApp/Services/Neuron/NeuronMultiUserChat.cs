using Waher.Runtime.Inventory;

namespace IdApp.Services.Neuron
{
    [Singleton]
    internal sealed class NeuronMultiUserChat : ServiceReferences, INeuronMultiUserChat
    {
        internal NeuronMultiUserChat()
            : base()
        {
        }
    }
}