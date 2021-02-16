using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.Services;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin
{
	/// <inheritdoc/>
	[Singleton]
	public class TagIdSdk : ITagIdSdk
	{
		private static ITagIdSdk instance;
		private readonly Assembly appAssembly;
		private FilesProvider databaseProvider;
		private Timer autoSaveTimer;

		private TagIdSdk(Assembly appAssembly, params DomainModel[] domains)
		{
			this.appAssembly = appAssembly;

			this.TagProfile = Types.InstantiateDefault<TagProfile>(false, (object)domains);
			this.LogService = Types.InstantiateDefault<LogService>(false, DependencyService.Resolve<IAppInformation>());
			this.uiDispatcher = Types.InstantiateDefault<UiDispatcher>(false);
			this.CryptoService = Types.InstantiateDefault<CryptoService>(false, this.LogService);
			this.NetworkService = Types.InstantiateDefault<NetworkService>(false, this.LogService, this.UiDispatcher);
			this.SettingsService = Types.InstantiateDefault<SettingsService>(false);
			this.StorageService = Types.InstantiateDefault<StorageService>(false);
			this.neuronService = Types.InstantiateDefault<NeuronService>(false, this.appAssembly, this.TagProfile, this.UiDispatcher, this.NetworkService, this.LogService);
			this.NavigationService = Types.InstantiateDefault<NavigationService>(false, this.LogService, this.uiDispatcher);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			instance = null;
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

			return instance ?? (instance = Types.InstantiateDefault<TagIdSdk>(false, appAssembly, domains));
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

			await InitializeDatabase(Thread?.CreateSubThread("DbStartup", ProfilerThreadType.Sequential));

			Thread?.NewState("Config");

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
			if (this.databaseProvider != null)
			{
				await this.databaseProvider.Flush();
				this.databaseProvider.Dispose();
				this.databaseProvider = null;
			}
		}

		/// <inheritdoc/>
		public async Task ShutdownInPanic()
		{
			StopAutoSaveTimer();
			this.uiDispatcher.IsRunningInTheBackground = false;
			await this.neuronService.UnloadFast();
			await Types.StopAllModules();
			Log.Terminate();
			if (this.databaseProvider != null)
			{
				await this.databaseProvider.Flush();
				this.databaseProvider.Dispose();
				this.databaseProvider = null;
			}
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

		private async Task InitializeDatabase(ProfilerThread Thread)
		{
			Thread?.Start();
			Thread?.NewState("Folders");

			string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dataFolder = Path.Combine(appDataFolder, "Data");

			string createDbMethod = $"{nameof(FilesProvider)}.{nameof(FilesProvider.CreateAsync)}()";
			Task<FilesProvider> CreateDatabaseFile()
			{
				return FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.CryptoService.GetCustomKey);
			}

			string method = null;
			try
			{
				Thread?.NewState("Provider");

				// 1. Try create database
				method = createDbMethod;
				this.databaseProvider = await CreateDatabaseFile();

				Thread?.NewState("Repair");

				method = nameof(FilesProvider.RepairIfInproperShutdown);
				await this.databaseProvider.RepairIfInproperShutdown(string.Empty);
			}
			catch (Exception e1)
			{
				e1 = Log.UnnestException(e1);
				Thread?.Exception(e1);

				// Create failed.
				this.LogService.LogException(e1, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

				try
				{
					Thread?.NewState("Repair2");

					// 2. Try repair database
					if (this.databaseProvider == null && Database.HasProvider)
					{
						// This is an attempt that _can_ work.
						// During a soft restart, there _may_ be a provider registered already. If so, grab it.
						this.databaseProvider = Database.Provider as FilesProvider;
					}

					if (this.databaseProvider == null)
					{
						// Reasoning: If we can't create a provider, and the database doesn't have one assigned either, we're in serious trouble.
						// Throw an exception, which is caught below, to try and perform a recovery.
						const string message = "Database does not have a provider, and one cannot be created because the Database file(s) are locked. Catch 22.";
						method = createDbMethod;
						throw new InvalidOperationException(message);
					}
					method = nameof(FilesProvider.RepairIfInproperShutdown);
					await this.databaseProvider.RepairIfInproperShutdown(string.Empty);
				}
				catch (Exception e2)
				{
					e2 = Log.UnnestException(e2);
					Thread?.Exception(e2);

					// Repair failed
					this.LogService.LogException(e2, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

					Thread?.NewState("UserAlert");

					if (await this.UiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseCorruptInfoText, AppResources.RepairAndContinue, AppResources.ContinueAnyway))
					{
						try
						{
							Thread?.NewState("Delete");

							// 3. Delete and create a new empty database
							method = "Delete database file(s) and create new empty database";
							Directory.Delete(dataFolder, true);

							Thread?.NewState("Recreate");

							this.databaseProvider = await CreateDatabaseFile();

							Thread?.NewState("Repair3");

							await this.databaseProvider.RepairIfInproperShutdown(string.Empty);
						}
						catch (Exception e3)
						{
							e3 = Log.UnnestException(e3);
							Thread?.Exception(e3);

							// Delete and create new failed. We're out of options.
							this.LogService.LogException(e3, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

							Thread?.NewState("DisplayAlert2");

							await this.UiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseRepairFailedInfoText, AppResources.Ok);
						}
					}
				}
			}

			try
			{
				Thread?.NewState("Register");

				method = $"{nameof(Database)}.{nameof(Database.Register)}";
				Database.Register(databaseProvider, false);
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				Thread?.Exception(e);
				this.LogService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));
			}

			Thread?.Stop();
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