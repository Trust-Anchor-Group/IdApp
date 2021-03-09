using System;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for Xmpp Chat functionality.
    /// </summary>
    [DefaultImplementation(typeof(NeuronChats))]
    public interface INeuronChats
    {
        /// <summary>
        /// Triggers whenever the chat functionality is online or not.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Returns <c>true</c> if Chats is online, <c>false</c> otherwise.
        /// </summary>
        bool IsOnline { get; }
    }
}