using System;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for chats.
    /// </summary>
    public interface INeuronChats : IDisposable
    {
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        bool IsOnline { get; }
    }
}