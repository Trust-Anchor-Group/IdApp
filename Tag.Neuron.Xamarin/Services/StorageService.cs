using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class StorageService : IStorageService
	{
		private FilesProvider databaseProvider;
		private readonly ILogService logService;
		private readonly ICryptoService cryptoService;
		private readonly IUiDispatcher uiDispatcher;
		private StorageState currentState;
		private TaskCompletionSource<StorageState> databaseTcs;
		private readonly string dataFolder;
        private Task initializeTask;

		/// <summary>
		/// Creates a new instance of the <see cref="StorageService"/> class.
		/// </summary>
		/// <param name="logService">The log service to use for logging.</param>
		/// <param name="cryptoService">The crypto service to use.</param>
		/// <param name="uiDispatcher">The UI Dispatcher, for main thread access and to display alerts.</param>
		public StorageService(ILogService logService, ICryptoService cryptoService, IUiDispatcher uiDispatcher)
		{
			this.logService = logService;
			this.cryptoService = cryptoService;
			this.uiDispatcher = uiDispatcher;
			string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.dataFolder = Path.Combine(appDataFolder, "Data");
            this.currentState = StorageState.NotInitialized;
            this.databaseTcs = new TaskCompletionSource<StorageState>();
        }

		#region LifeCycle management

		/// <inheritdoc />
		public void Init(ProfilerThread Thread)
		{
			if (this.currentState == StorageState.NotInitialized)
            {
                this.currentState = StorageState.Initializing;
                this.initializeTask = InitializeAsync(Thread);
            }
		}

		/// <inheritdoc />
		public Task<StorageState> WaitForReadyState()
		{
			if (this.currentState == StorageState.Ready || this.currentState == StorageState.NeedsRepair)
            {
                return Task.FromResult(this.currentState);
            }

            return this.databaseTcs.Task;
		}

		/// <inheritdoc />
		public async Task TryRepairDatabase(ProfilerThread Thread)
		{
			if (await this.uiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseCorruptInfoText, AppResources.RepairAndContinue, AppResources.ContinueAnyway))
			{
				string method = "Delete database file(s) and create new empty database";
				try
				{
					Thread?.NewState("Delete");

					// 3. Delete and create a new empty database
					Directory.Delete(dataFolder, true);

					Thread?.NewState("Recreate");

					this.databaseProvider = await CreateDatabaseFile();

					Thread?.NewState("Repair3");

					await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

					// If we had to repair, we must register the provider 'again', as one hasn't been provided yet.
					if (!Database.HasProvider)
                    {
                        method = $"{nameof(Database)}.{nameof(Database.Register)}";
                        Database.Register(databaseProvider, false);
                    }
                    // All is good.
                    this.SetState(StorageState.Ready);
                }
                catch (Exception e3)
				{
					e3 = Log.UnnestException(e3);
					Thread?.Exception(e3);

					// Delete and create new failed. We're out of options.
					this.logService.LogException(e3, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

					Thread?.NewState("DisplayAlert2");

					await this.uiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseRepairFailedInfoText, AppResources.Ok);
				}
			}
		}

		/// <inheritdoc />
		public async Task Shutdown()
		{
            this.currentState = StorageState.NotInitialized;

			try
			{
                if (!(this.databaseProvider is null))
                {
                    await this.databaseProvider.Flush();
                    this.databaseProvider.Dispose();
                    this.databaseProvider = null;
                }
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
		}

		private Task<FilesProvider> CreateDatabaseFile()
		{
			return FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.cryptoService.GetCustomKey);
		}

		private async Task InitializeAsync(ProfilerThread Thread)
		{
			string createDbMethod = $"{nameof(FilesProvider)}.{nameof(FilesProvider.CreateAsync)}()";
			string method = null;

			Thread?.Start();
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
				this.logService.LogException(e1, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));

				try
				{
					Thread?.NewState("Repair2");

					// 2. Try repair database
					if (this.databaseProvider is null && Database.HasProvider)
					{
						// This is an attempt that _can_ work.
						// During a soft restart, there _may_ be a provider registered already. If so, grab it.
						this.databaseProvider = Database.Provider as FilesProvider;
					}

					if (this.databaseProvider is null)
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
					this.logService.LogException(e2, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));
				}
			}

			try
			{
				Thread?.NewState("Register");

				if (databaseProvider != null)
                {
                    method = $"{nameof(Database)}.{nameof(Database.Register)}";
                    Database.Register(databaseProvider, false);
                    // All is good.
					this.SetState(StorageState.Ready);
                }
                else
                {
                    this.SetState(StorageState.NeedsRepair);
                }
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				Thread?.Exception(e);
				this.logService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), method));
				this.SetState(StorageState.NeedsRepair);
			}
			finally
			{
				Thread?.Stop();
			}
		}

        private void SetState(StorageState state)
        {
            this.currentState = state;
            this.databaseTcs.TrySetResult(state);
			// The Tcs task is now spent, create a new one.
            this.databaseTcs = new TaskCompletionSource<StorageState>();
        }
		#endregion

		public Task Insert(object obj)
		{
			return Database.Insert(obj);
		}

		public Task Update(object obj)
		{
			return Database.Update(obj);
		}

		public Task<T> FindFirstDeleteRest<T>() where T : class
		{
			return Database.FindFirstDeleteRest<T>();
		}

		public Task<T> FindFirstIgnoreRest<T>() where T : class
		{
			return Database.FindFirstIgnoreRest<T>();
		}

		public Task Export(IDatabaseExport exportOutput)
		{
			return Database.Export(exportOutput);
		}
	}
}
