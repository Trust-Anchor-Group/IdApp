using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin
{
    /// <summary>
    /// The TagIdSdk is the 'root' of the Neuron library.
    /// Use this to access Neuron specific features and services, and to control startup/shutdown.
    /// <br/>
    /// It is imperative that you integrate this class into your <see cref="Application"/>
    /// <see cref="Application.OnStart"/> and <see cref="Application.OnResume"/> methods.
    /// </summary>
    [DefaultImplementation(typeof(TagIdSdk))]
    public interface ITagIdSdk : IDisposable
    {
        /// <summary>
        /// To be called when the app is starting, regardless of whether it a cold start, or if it is resuming.
        /// </summary>
        /// <param name="isResuming">Set to <c>true</c> if the app is resuming, <c>false</c> otherwise.</param>
        /// <param name="Thread">Optional Profiler Thread, for profiling startup times. May be null.</param>
        Task Startup(bool isResuming, ProfilerThread Thread);
        /// <summary>
        /// To be called when the app is shutting down.
        /// </summary>
        /// <param name="keepRunningInTheBackground">Set to <c>true</c> if the app should maintain the Neuron server connection, <c>false</c> otherwise.</param>
        /// <returns></returns>
        Task Shutdown(bool keepRunningInTheBackground);
        /// <summary>
        /// To be called when the app is shutting down due to an unhandled exception. Will exit as fast as possible.
        /// </summary>
        /// <returns></returns>
        Task ShutdownInPanic();
        /// <summary>
        /// The TAG Profile used for authentication/connection.
        /// </summary>
        ITagProfile TagProfile { get; }
        /// <summary>
        /// A UI dispatcher for accessing the UI thread as well as display alerts to the user.
        /// </summary>
        IUiDispatcher UiDispatcher { get; }
        /// <summary>
        /// Provides authorization services.
        /// </summary>
        ICryptoService CryptoService { get; }
        /// <summary>
        /// Provides Neuron server access.
        /// </summary>
        INeuronService NeuronService { get; }
        /// <summary>
        /// Provides network access.
        /// </summary>
        INetworkService NetworkService { get; }
        /// <summary>
        /// The log service, collecting information about events, exceptions and the likes.
        /// </summary>
        ILogService LogService { get; }
        /// <summary>
        /// Use this for persistent storage.
        /// </summary>
        IStorageService StorageService { get; }
        /// <summary>
        /// Allows for saving/restoring user settings.
        /// </summary>
        ISettingsService SettingsService { get; }
        /// <summary>
        /// Provides application navigation services in a generic way.
        /// </summary>
        INavigationService NavigationService { get; }
    }
}