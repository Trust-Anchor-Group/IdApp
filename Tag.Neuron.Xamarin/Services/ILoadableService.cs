using System;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// A service that can be loaded and unloaded at will. Typically during startup and shutdown of an application.
    /// </summary>
    public interface ILoadableService
    {
        /// <summary>
        /// Loads the specified service.
        /// </summary>
        /// <param name="isResuming">Set to <c>true</c> to indicate the app is resuming as opposed to starting cold.</param>
        /// <returns></returns>
        Task Load(bool isResuming);
        /// <summary>
        /// Unloads the specified service.
        /// </summary>
        /// <returns></returns>
        Task Unload();
        /// <summary>
        /// Fires whenever the loading state of the service changes.
        /// </summary>
        event EventHandler<LoadedEventArgs> Loaded;
    }
}