using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    public interface IStorageService
    {
        Task Insert(object obj);
        Task Update(object obj);
        Task<T> FindFirstDeleteRest<T>() where T : class;
        Task<T> FindFirstIgnoreRest<T>() where T : class;
    }
}