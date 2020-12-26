using System;

namespace Tag.Sdk.Core.Services
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