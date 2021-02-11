using System;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    /// <inheritdoc/>
    public class LoadableService : ILoadableService
    {
        /// <summary>
        /// Gets whether the service is unloading or not.
        /// </summary>
        public bool IsUnloading { get; protected set; }
        /// <summary>
        /// Gets whether the service is loading or not.
        /// </summary>
        public bool IsLoading { get; protected set; }
        /// <summary>
        /// Gets whether the service is loaded.
        /// </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary>
        /// Sets the <see cref="IsLoading"/> flag if the service isn't already loading.
        /// </summary>
        /// <returns><c>true</c> if the service will load, <c>false</c> otherwise.</returns>
        protected bool BeginLoad()
        {
            if (!this.IsLoaded && !this.IsLoading)
            {
                IsLoading = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the <see cref="IsLoading"/> and <see cref="IsLoaded"/> flags and fires an event
        /// representing the current load state of the service.
        /// </summary>
        /// <param name="isLoaded">The current loaded state to set.</param>
        protected void EndLoad(bool isLoaded)
        {
            IsLoading = false;
            IsLoaded = isLoaded;
            OnLoaded(new LoadedEventArgs(IsLoaded));
        }

        /// <summary>
        /// Sets the <see cref="IsLoading"/> flag if the service isn't already unloading.
        /// </summary>
        /// <returns><c>true</c> if the service will unload, <c>false</c> otherwise.</returns>
        protected bool BeginUnload()
        {
            if (this.IsLoaded && !this.IsUnloading)
            {
                IsUnloading = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the <see cref="IsLoading"/> and <see cref="IsLoaded"/> flags and fires an event
        /// representing the current load state of the service.
        /// </summary>
        protected void EndUnload()
        {
            IsUnloading = false;
            IsLoaded = false;
            OnLoaded(new LoadedEventArgs(IsLoaded));
        }

        /// <inheritdoc/>
        public virtual Task Load(bool isResuming)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task Unload()
        {
            return Task.CompletedTask;
        }

        private event EventHandler<LoadedEventArgs> loaded;

        /// <inheritdoc/>
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