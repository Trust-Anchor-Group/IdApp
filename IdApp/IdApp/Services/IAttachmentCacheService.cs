using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Tag.Neuron.Xamarin.Services;

namespace IdApp.Services
{
    /// <summary>
    /// Represents a service that caches images downloaded via http calls.
    /// They are stored for a certain duration.
    /// </summary>
    [DefaultImplementation(typeof(AttachmentCacheService))]
    public interface IAttachmentCacheService : ILoadableService
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
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
		/// <param name="Permanent">If attachment is permanent or temporary.</param>
        /// <param name="Data">Binary data of image</param>
        /// <param name="ContentType">Content-Type of data.</param>
        Task Add(string Url, string ParentId, bool Permanent, byte[] Data, string ContentType);

        /// <summary>
        /// Makes items in the cache, belonging to a given parent object, temporary.
        /// </summary>
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
        Task MakeTemporary(string ParentId);

        /// <summary>
        /// Makes items in the cache, belonging to a given parent object, permanent.
        /// </summary>
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
        Task MakePermanent(string ParentId);
    }
}