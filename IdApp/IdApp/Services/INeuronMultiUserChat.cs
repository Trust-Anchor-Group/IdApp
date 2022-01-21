using IdApp.Services.Neuron;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    /// <summary>
    /// Adds support for Xmpp Multi-User Chat functionality.
    /// </summary>
    [DefaultImplementation(typeof(NeuronMultiUserChat))]
    public interface INeuronMultiUserChat
    {
    }
}