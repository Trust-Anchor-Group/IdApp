using System.IO;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Tag.Neuron.Xamarin.Services;

namespace IdApp.Services
{
    [DefaultImplementation(typeof(ImageCacheService))]
    public interface IImageCacheService : ILoadableService
    {
        bool TryGet(string url, out Stream stream);
        Task Add(string url, Stream stream);
    }
}