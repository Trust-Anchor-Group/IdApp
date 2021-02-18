﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.Services;
using Waher.Events;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin
{
	/// <inheritdoc/>
	[Singleton]
	public class TagIdSdk : ITagIdSdk
	{
		private static ITagIdSdk Instance;
		private Timer autoSaveTimer;

		private TagIdSdk(Assembly appAssembly, params DomainModel[] domains)
		{
			this.TagProfile = Types.InstantiateDefault<TagProfile>(false, (object)domains);
			this.LogService = Types.InstantiateDefault<LogService>(false, DependencyService.Resolve<IAppInformation>());
			this.uiDispatcher = Types.InstantiateDefault<UiDispatcher>(false);
			this.CryptoService = Types.InstantiateDefault<CryptoService>(false, this.LogService);
			this.NetworkService = Types.InstantiateDefault<NetworkService>(false, this.LogService, this.UiDispatcher);
			this.SettingsService = Types.InstantiateDefault<SettingsService>(false);
			this.StorageService = Types.InstantiateDefault<StorageService>(false);
			this.neuronService = Types.InstantiateDefault<NeuronService>(false, appAssembly, this.TagProfile, this.UiDispatcher, this.NetworkService, this.LogService);
			this.NavigationService = Types.InstantiateDefault<NavigationService>(false, this.LogService, this.uiDispatcher);
		}

		/// <summary>
		/// Creates an instance of the <see cref="ITagIdSdk"/>. This is a factory method.
		/// </summary>
		/// <param name="appAssembly"></param>
		/// <param name="domains"></param>
		/// <returns></returns>
		public static ITagIdSdk Create(Assembly appAssembly, params DomainModel[] domains)
		{
			if (appAssembly is null)
				throw new ArgumentException("Value cannot be null", nameof(appAssembly));

			return Instance ?? (Instance = Types.InstantiateDefault<TagIdSdk>(false, appAssembly, domains));
		}

		/// <inheritdoc/>
		public ITagProfile TagProfile { get; }
		private readonly UiDispatcher uiDispatcher;
		/// <inheritdoc/>
		public IUiDispatcher UiDispatcher => this.uiDispatcher;
		/// <inheritdoc/>
		public ICryptoService CryptoService { get; }
		private readonly IInternalNeuronService neuronService;
		/// <inheritdoc/>
		public INeuronService NeuronService => this.neuronService;
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

		/// <inheritdoc/>
		public async Task Startup(bool isResuming, ProfilerThread Thread)
		{
			Thread?.Start();
			Thread?.NewState("DB");

			this.uiDispatcher.IsRunningInTheBackground = false;

            // Start the db
            this.StorageService.Init(Thread);
            StorageState dbState = await this.StorageService.WaitForReadyState();
			if (dbState == StorageState.NeedsRepair)
            {
                await this.StorageService.TryRepairDatabase(Thread);
            }

			if (!isResuming)
			{
				await CreateOrRestoreConfiguration();
			}

			Thread?.NewState("Load");

			await this.NeuronService.Load(isResuming);

			Thread?.NewState("Timer");

			TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
			this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

			Thread?.Stop();
		}

        /// <inheritdoc/>
		public async Task Shutdown(bool keepRunningInTheBackground)
		{
			StopAutoSaveTimer();
			this.uiDispatcher.IsRunningInTheBackground = true;
			if (!keepRunningInTheBackground)
			{
				await this.neuronService.Unload();
			}
			await Types.StopAllModules();
			Log.Terminate();
            await this.StorageService.Shutdown();
		}

		/// <inheritdoc/>
		public async Task ShutdownInPanic()
		{
			StopAutoSaveTimer();
			this.uiDispatcher.IsRunningInTheBackground = false;
			await this.neuronService.UnloadFast();
			await Types.StopAllModules();
			Log.Terminate();
            await this.StorageService.Shutdown();
		}

		private void StopAutoSaveTimer()
		{
			if (this.autoSaveTimer != null)
			{
				this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
				this.autoSaveTimer.Dispose();
				this.autoSaveTimer = null;
			}
		}

		private async Task AutoSave()
		{
			if (this.TagProfile.IsDirty)
			{
				this.TagProfile.ResetIsDirty();
				try
				{
					TagConfiguration tc = this.TagProfile.ToConfiguration();
					try
					{
						await this.StorageService.Update(tc);
					}
					catch (KeyNotFoundException)
					{
						await this.StorageService.Insert(tc);
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}
		}

        private async Task CreateOrRestoreConfiguration()
		{
			TagConfiguration configuration;

			try
			{
				configuration = await this.StorageService.FindFirstDeleteRest<TagConfiguration>();
			}
			catch (Exception findException)
			{
				this.LogService.LogException(findException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				configuration = null;
			}

			if (configuration == null)
			{
				configuration = new TagConfiguration();
				try
				{
					await this.StorageService.Insert(configuration);
				}
				catch (Exception insertException)
				{
					this.LogService.LogException(insertException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			this.TagProfile.FromConfiguration(configuration);
		}
	}
}