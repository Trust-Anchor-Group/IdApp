using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Wraps the <see cref="Database"/> for easy access to persistent encrypted storage. Use this for any sensitive data.
    /// </summary>
    [DefaultImplementation(typeof(StorageService))]
    public interface IStorageService
    {
        /// <summary>
        /// Inserts an object into the database.
        /// </summary>
        /// <param name="obj">The object to store.</param>
        /// <returns></returns>
        Task Insert(object obj);
        /// <summary>
        /// Updates an object in the database.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        /// <returns></returns>
        Task Update(object obj);
        /// <summary>
        /// Returns the first match (if any) of the given type. Deletes the other matching entries.
        /// </summary>
        /// <typeparam name="T">The type of the objects to retrieve.</typeparam>
        /// <returns></returns>
        Task<T> FindFirstDeleteRest<T>() where T : class;
        /// <summary>
        /// Returns the first match (if any) of the given type. Ignores (leaves) the other matching entries.
        /// </summary>
        /// <typeparam name="T">The type of the objects to retrieve.</typeparam>
        /// <returns></returns>
        Task<T> FindFirstIgnoreRest<T>() where T : class;
    }
}