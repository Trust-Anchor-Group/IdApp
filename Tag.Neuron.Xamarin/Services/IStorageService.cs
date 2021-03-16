using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Persistence;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Wraps the <see cref="Database"/> for easy access to persistent encrypted storage. Use this for any sensitive data.
    /// </summary>
    [DefaultImplementation(typeof(StorageService))]
    public interface IStorageService
    {
        #region LifeCycle management

        /// <summary>
        /// Initializes the persistent storage on a background task. This call is asynchronous.
        /// </summary>
        /// <param name="Thread"></param>
        void Init(ProfilerThread Thread);
        /// <summary>
        /// Returns a task so the persistent storage's ready state can be awaited.
        /// </summary>
        /// <returns></returns>
        Task<StorageState> WaitForReadyState();
        /// <summary>
        /// Tries to repair the database if something went wrong. Alerts the user if needed.
        /// </summary>
        /// <param name="Thread"></param>
        /// <returns></returns>
        Task TryRepairDatabase(ProfilerThread Thread);
        /// <summary>
        /// Shuts down this persistent storage instance.
        /// </summary>
        /// <returns></returns>
        Task Shutdown();

        #endregion

        /// <summary>
        /// Inserts an object into the database.
        /// </summary>
        /// <param name="obj">The object to store.</param>
        Task Insert(object obj);

        /// <summary>
        /// Updates an object in the database.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        Task Update(object obj);
        
        /// <summary>
        /// Returns the first match (if any) of the given type. Deletes the other matching entries.
        /// </summary>
        /// <typeparam name="T">The type of the objects to retrieve.</typeparam>
        Task<T> FindFirstDeleteRest<T>() where T : class;
        
        /// <summary>
        /// Returns the first match (if any) of the given type. Ignores (leaves) the other matching entries.
        /// </summary>
        /// <typeparam name="T">The type of the objects to retrieve.</typeparam>
        Task<T> FindFirstIgnoreRest<T>() where T : class;

        /// <summary>
        /// Exports the contents of the database to <paramref name="exportOutput"/>.
        /// </summary>
        /// <param name="exportOutput">Receives the contents of the database.</param>
        Task Export(IDatabaseExport exportOutput);
    }

    /// <summary>
    /// Represents the different states persistent storage can be in.
    /// </summary>
    public enum StorageState
    {
        /// <summary>
        /// Persistent storage has not yet been initialized.
        /// </summary>
        NotInitialized,

        /// <summary>
        /// Persistent storage is currently being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// Persistent storage is ready.
        /// </summary>
        Ready,
        
        /// <summary>
        /// Persistent storage is experiencing failures, and needs repair.
        /// </summary>
        NeedsRepair
    }
}