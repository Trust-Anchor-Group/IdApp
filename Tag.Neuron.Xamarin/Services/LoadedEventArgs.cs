using System;

namespace Tag.Neuron.Xamarin.Services
{
    public sealed class LoadedEventArgs : EventArgs
    {
        public LoadedEventArgs(bool isLoaded)
        {
            IsLoaded = isLoaded;
        }

        public bool IsLoaded { get; }
    }
}