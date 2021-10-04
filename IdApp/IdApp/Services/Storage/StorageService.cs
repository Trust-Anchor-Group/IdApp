using IdApp.Services.Crypto;
using IdApp.Services.EventLog;
using IdApp.Services.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace IdApp.Services.Storage
{
	[Singleton]
	internal sealed class StorageService : IStorageService
	{
		private readonly LinkedList<TaskCompletionSource<bool>> tasksWaiting = new LinkedList<TaskCompletionSource<bool>>();
		private readonly ILogService logService;
		private readonly ICryptoService cryptoService;
		private readonly IUiDispatcher uiDispatcher;
		private readonly string dataFolder;
		private FilesProvider databaseProvider;
		private bool? initialized = null;
		private bool started = false;

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
		}

		#region LifeCycle management

		/// <inheritdoc />
		public async Task Init(ProfilerThread Thread)
		{
			lock (this.tasksWaiting)
			{
				if (this.started)
					return;

				this.started = true;
			}

			Thread?.Start();
			try
			{
				Thread?.NewState("Provider");
				if (Database.HasProvider)
					this.databaseProvider = Database.Provider as FilesProvider;

				if (this.databaseProvider is null)
				{
					this.databaseProvider = await CreateDatabaseFile(Thread);

					Thread?.NewState("CheckDB");
					await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

					await this.databaseProvider.Start();
				}
			}
			catch (Exception e1)
			{
				e1 = Log.UnnestException(e1);
				Thread?.Exception(e1);
				this.logService.LogException(e1);
			}

			try
			{
				Thread?.NewState("Register");

				if (!(databaseProvider is null))
				{
					Database.Register(databaseProvider, false);
					this.InitDone(true);
					Thread?.Stop();
					return;
				}
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				Thread?.Exception(e);
				this.logService.LogException(e);
			}

			try
			{
				Thread?.NewState("UI");
				if (await this.uiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseCorruptInfoText, AppResources.RepairAndContinue, AppResources.ContinueAnyway))
				{
					try
					{
						Thread?.NewState("Delete");
						Directory.Delete(dataFolder, true);

						Thread?.NewState("Recreate");
						this.databaseProvider = await CreateDatabaseFile(Thread);

						Thread?.NewState("Repair");
						await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

						await this.databaseProvider.Start();

						if (!Database.HasProvider)
						{
							Thread?.NewState("Register");
							Database.Register(databaseProvider, false);
							this.InitDone(true);
							Thread?.Stop();
							return;
						}
					}
					catch (Exception e3)
					{
						e3 = Log.UnnestException(e3);
						Thread?.Exception(e3);
						this.logService.LogException(e3);

						Thread?.NewState("UI");
						await this.uiDispatcher.DisplayAlert(AppResources.DatabaseIssue, AppResources.DatabaseRepairFailedInfoText, AppResources.Ok);
					}
				}

				this.InitDone(false);
			}
			finally
			{
				Thread?.Stop();
			}
		}

		private void InitDone(bool Result)
		{
			lock (this.tasksWaiting)
			{
				this.initialized = Result;

				foreach (TaskCompletionSource<bool> Wait in this.tasksWaiting)
					Wait.TrySetResult(Result);

				this.tasksWaiting.Clear();
			}
		}

		/// <inheritdoc />
		public Task<bool> WaitInitDone()
		{
			lock (this.tasksWaiting)
			{
				if (this.initialized.HasValue)
					return Task.FromResult<bool>(this.initialized.Value);

				TaskCompletionSource<bool> Wait = new TaskCompletionSource<bool>();
				this.tasksWaiting.AddLast(Wait);

				return Wait.Task;
			}
		}

		/// <inheritdoc />
		public async Task Shutdown()
		{
			lock (this.tasksWaiting)
			{
				this.initialized = null;
				this.started = false;
			}

			try
			{
				if (!(this.databaseProvider is null))
				{
					Database.Register(new NullDatabaseProvider(), false);
					await this.databaseProvider.Flush();
					await this.databaseProvider.Stop();
					this.databaseProvider = null;
				}
			}
			catch (Exception e)
			{
				this.logService.LogException(e);
			}
		}

		private Task<FilesProvider> CreateDatabaseFile(ProfilerThread Thread)
		{
			FilesProvider.AsyncFileIo = false;  // Asynchronous file I/O induces a long delay during startup on mobile platforms. Why??

			return FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8,
				(int)Constants.Timeouts.Database.TotalMilliseconds, this.cryptoService.GetCustomKey, Thread);
		}

		#endregion

		public async Task Insert(object obj)
		{
			await Database.Insert(obj);
			await Database.Provider.Flush();
		}

		public async Task Update(object obj)
		{
			await Database.Update(obj);
			await Database.Provider.Flush();
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
