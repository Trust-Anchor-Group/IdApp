using System;

namespace XamarinApp.Services
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