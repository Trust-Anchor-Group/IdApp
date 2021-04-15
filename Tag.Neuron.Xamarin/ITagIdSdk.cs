using System;
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
    public interface ITagIdSdk
    {
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

        /// <summary>
        /// Profiler of startup process, if any.
        /// </summary>
        Profiler StartupProfiler { get; }
    }
}