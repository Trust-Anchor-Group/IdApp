using System;
using System.Reflection;
using IdApp.Services;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Xamarin.Forms;

namespace IdApp
{
	/// <summary>
	/// The TagIdSdk is the 'root' of the Neuron library.
	/// Use this to access Neuron specific features and services, and to control startup/shutdown.
	/// <br/>
	/// It is imperative that you integrate this class into your <see cref="Application"/>
	/// <see cref="Application.OnStart"/> and <see cref="Application.OnResume"/> methods.
	/// </summary>
	[Singleton]
	public class TagIdSdk : ITagIdSdk
	{
		/// <summary>
		/// The TagIdSdk is the 'root' of the Neuron library.
		/// Use this to access Neuron specific features and services, and to control startup/shutdown.
		/// <br/>
		/// It is imperative that you integrate this class into your <see cref="Application"/>
		/// <see cref="Application.OnStart"/> and <see cref="Application.OnResume"/> methods.
		/// </summary>
		/// <param name="appAssembly">The assembly containing the main App class.</param>
		/// <param name="startupProfiler">Optional Startup profiler. May be null.</param>
		public TagIdSdk(Assembly appAssembly, Profiler startupProfiler)
		{
			this.StartupProfiler = startupProfiler;

			this.TagProfile = Types.InstantiateDefault<ITagProfile>(false);
			this.LogService = Types.InstantiateDefault<ILogService>(false);
			this.UiDispatcher = Types.InstantiateDefault<IUiDispatcher>(false);
			this.CryptoService = Types.InstantiateDefault<ICryptoService>(false, this.LogService);
			this.NetworkService = Types.InstantiateDefault<INetworkService>(false, this.LogService, this.UiDispatcher);
			this.StorageService = Types.InstantiateDefault<IStorageService>(false, this.LogService, this.CryptoService, this.UiDispatcher);
			this.SettingsService = Types.InstantiateDefault<ISettingsService>(false, this.StorageService, this.LogService);
			this.NavigationService = Types.InstantiateDefault<INavigationService>(false, this.LogService, this.UiDispatcher);
			this.NeuronService = Types.InstantiateDefault<INeuronService>(false, appAssembly, this.TagProfile, this.UiDispatcher, this.NetworkService, this.LogService, this.SettingsService, startupProfiler);
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
	}
}