using IdApp.Services.Neuron;
using System;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    /// <summary>
    /// Adds support for Xmpp Multi-User Chat functionality.
    /// </summary>
    [DefaultImplementation(typeof(NeuronMultiUserChat))]
    public interface INeuronMultiUserChat
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