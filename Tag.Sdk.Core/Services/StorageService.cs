using System.Threading.Tasks;
using Waher.Persistence;

namespace Tag.Sdk.Core.Services
{
    internal sealed class StorageService : IStorageService
    {
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
    }
}