using System;
using System.Threading.Tasks;

namespace XamarinApp.Services
{
    internal class LoadableService : ILoadableService
    {
        public bool IsUnloading { get; protected set; }
        public bool IsLoading { get; protected set; }
        public bool IsLoaded { get; protected set; }

        protected void BeginLoad()
        {
            IsLoading = true;
        }

        protected void EndLoad(bool isLoaded)
        {
            IsLoading = false;
            IsLoaded = isLoaded;
            OnLoaded(new LoadedEventArgs(IsLoaded));
        }

        protected void BeginUnload()
        {
            IsUnloading = true;
        }

        protected void EndUnload()
        {
            IsUnloading = false;
            IsLoaded = false;
            OnLoaded(new LoadedEventArgs(IsLoaded));
        }

        public virtual Task Load()
        {
            return Task.CompletedTask;
        }

        public virtual Task Unload()
        {
            return Task.CompletedTask;
        }

        private event EventHandler<LoadedEventArgs> loaded;

        public event EventHandler<LoadedEventArgs> Loaded
        {
            add
            {
                loaded += value;
                value(this, new LoadedEventArgs(IsLoaded));
            }
            remove
            {
                loaded -= value;
            }
        }

        private void OnLoaded(LoadedEventArgs e)
        {
            loaded?.Invoke(this, e);
        }
    }
}