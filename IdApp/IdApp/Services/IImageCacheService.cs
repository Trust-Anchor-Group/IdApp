using System.IO;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Tag.Neuron.Xamarin.Services;

namespace IdApp.Services
{
    /// <summary>
    /// Represents a service that caches images downloaded via http calls.
    /// They are stored for a certain duration.
    /// </summary>
    [DefaultImplementation(typeof(ImageCacheService))]
    public interface IImageCacheService : ILoadableService
    {
        /// <summary>
        /// Tries to get a cached image given the specified url.
        /// </summary>
        /// <param name="url">The url of the image to get.</param>
        /// <param name="stream">The stream pointing to the cached image if it is available.</param>
        /// <returns></returns>
        bool TryGet(string url, out MemoryStream stream);
        /// <summary>
        /// Adds an image to the cache.
        /// </summary>
        /// <param name="url">The url, which is the key for accessing it later.</param>
        /// <param name="stream">The image stream to store.</param>
        /// <returns></returns>
        Task Add(string url, Stream stream);
        /// <summary>
        /// Explicitly invalidates the cache for a certain image, even though the expiry time hasn't passed.
        /// </summary>
        /// <param name="url">The url of the image.</param>
        /// <returns></returns>
        void Invalidate(string url);
    }
}