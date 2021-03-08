﻿using System;
using Tag.Neuron.Xamarin.Services;
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

        /// <summary>
        /// Registers a singleton implementation instance for later resolve.
        /// </summary>
        /// <typeparam name="TFrom">The interface it implements.</typeparam>
        /// <typeparam name="TTo">The implementation class.</typeparam>
        /// <param name="singleton">The singleton instance to register.</param>
        void RegisterSingleton<TFrom, TTo>(TFrom singleton)
            where TTo : TFrom
            where TFrom : class;

        /// <summary>
        /// Resolves the type <b>t</b> using an IoC pattern.
        /// </summary>
        /// <param name="t">The type to resolve.</param>
        /// <returns>The resolved type, or <c>null</c>.</returns>
        object Resolve(Type t);
    }
}