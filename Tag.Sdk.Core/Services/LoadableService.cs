using System;
using System.Threading.Tasks;

namespace Tag.Sdk.Core.Services
{
    public class LoadableService : ILoadableService
    {
        public bool IsUnloading { get; protected set; }
        public bool IsLoading { get; protected set; }
        public bool IsLoaded { get; protected set; }

        protected bool BeginLoad()
        {
            if (!this.IsLoaded && !this.IsLoading)
            {
                IsLoading = true;
                return true;
            }

            return false;
        }

        protected void EndLoad(bool isLoaded)
        {
            IsLoading = false;
            IsLoaded = isLoaded;
            OnLoaded(new LoadedEventArgs(IsLoaded));
        }

        protected bool BeginUnload()
        {
            if (this.IsLoaded && !this.IsUnloading)
            {
                IsUnloading = true;
                return true;
            }

            return false;
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