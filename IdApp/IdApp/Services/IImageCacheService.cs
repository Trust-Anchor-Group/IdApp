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
        /// <param name="Url">The url of the image to get.</param>
        /// <returns>If entry was found in the cache, the binary data of the image together with the Content-Type of the data.</returns>
        Task<(byte[], string)> TryGet(string Url);

        /// <summary>
        /// Adds an image to the cache.
        /// </summary>
        /// <param name="Url">The url, which is the key for accessing it later.</param>
        /// <param name="Data">Binary data of image</param>
        /// <param name="ContentType">Content-Type of data.</param>
        Task Add(string Url, byte[] Data, string ContentType);
    }
}