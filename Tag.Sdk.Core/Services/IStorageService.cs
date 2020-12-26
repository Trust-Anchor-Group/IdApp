using System.Threading.Tasks;

namespace Tag.Sdk.Core.Services
{
    public interface IStorageService
    {
        Task Insert(object obj);
        Task Update(object obj);
        Task<T> FindFirstDeleteRest<T>() where T : class;
        Task<T> FindFirstIgnoreRest<T>() where T : class;
    }
}