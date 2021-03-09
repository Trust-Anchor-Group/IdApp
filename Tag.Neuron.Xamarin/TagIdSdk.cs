using System;
using System.Collections.Generic;
using System.Reflection;
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Profiling;

namespace Tag.Neuron.Xamarin
{
    /// <inheritdoc/>
    public class TagIdSdk : ITagIdSdk
    {
        private static ITagIdSdk _instance;
        private readonly Dictionary<Type, object> singletons;

        private TagIdSdk(Assembly appAssembly, Profiler startupProfiler, params DomainModel[] domains)
        {
            singletons = new Dictionary<Type, object>();
            this.StartupProfiler = startupProfiler;
            this.TagProfile = new TagProfile(domains);
            this.singletons.Add(typeof(ITagProfile), this.TagProfile);
            this.LogService = new LogService();
            this.singletons.Add(typeof(ILogService), this.LogService);
            this.UiDispatcher = new UiDispatcher();
            this.singletons.Add(typeof(IUiDispatcher), this.UiDispatcher);
            this.CryptoService = new CryptoService(this.LogService);
            this.singletons.Add(typeof(ICryptoService), this.CryptoService);
            this.NetworkService = new NetworkService(this.LogService, this.UiDispatcher);
            this.singletons.Add(typeof(INetworkService), this.NetworkService);
            this.SettingsService = new SettingsService();
            this.singletons.Add(typeof(ISettingsService), this.SettingsService);
            this.StorageService = new StorageService(this.LogService, this.CryptoService, this.UiDispatcher);
            this.singletons.Add(typeof(IStorageService), this.StorageService);
            this.NavigationService = new NavigationService(this.LogService, this.UiDispatcher);
            this.singletons.Add(typeof(INavigationService), this.NavigationService);
            this.NeuronService = new NeuronService(appAssembly, this.TagProfile, this.UiDispatcher, this.NetworkService, this.LogService, startupProfiler);
            this.singletons.Add(typeof(INeuronService), this.NeuronService);
        }

        /// <summary>
        /// Creates an instance of the <see cref="ITagIdSdk"/>. This is a factory method.
        /// </summary>
        /// <param name="appAssembly">The assembly containing the main App class.</param>
        /// <param name="startupProfiler">Optional Startup profiler. May be null.</param>
        /// <param name="domains">Featured domains.</param>
        /// <returns>Tag ID SDK instance reference.</returns>
        public static ITagIdSdk Create(Assembly appAssembly, Profiler startupProfiler, params DomainModel[] domains)
        {
            return _instance ?? (_instance = new TagIdSdk(appAssembly, startupProfiler, domains));
        }

        /// <inheritdoc/>
        public ITagProfile TagProfile { get; }

        /// <inheritdoc/>
        public IUiDispatcher UiDispatcher { get; }

        /// <inheritdoc/>
        public ICryptoService CryptoService { get; }

        /// <inheritdoc/>
        public INeuronService NeuronService { get; }

        /// <inheritdoc/>
        public INetworkService NetworkService { get; }

        /// <inheritdoc/>
        public INavigationService NavigationService { get; }

        /// <inheritdoc/>
        public IStorageService StorageService { get; }

        /// <inheritdoc/>
        public ISettingsService SettingsService { get; }

        /// <inheritdoc/>
        public ILogService LogService { get; }

        /// <summary>
        /// Profiler of startup process, if any.
        /// </summary>
        public Profiler StartupProfiler { get; }

        /// <inheritdoc/>
        public void RegisterSingleton<TFrom, TTo>(TFrom singleton) 
            where TTo : TFrom
            where TFrom : class
        {
            if (!(singleton is null))
            {
                Type t = typeof(TFrom);
                this.singletons[t] = singleton;
            }
        }

        /// <summary>
        /// A wafer-thin IoC implementation for the services provided by the <see cref="ITagIdSdk"/>.
        /// </summary>
        /// <param name="t">The type to resolve.</param>
        /// <returns>The resolved type, if found, or <c>null</c>.</returns>
        public object Resolve(Type t)
        {
            if (this.singletons.TryGetValue(t, out object obj))
            {
                return obj;
            }

            return null;
        }
    }
}