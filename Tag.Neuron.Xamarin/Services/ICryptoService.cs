using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Cryptographic service that helps create passwords and other security related tasks.
    /// </summary>
    public interface ICryptoService
    {
        /// <summary>
        /// Returns a cryptographic authorization key for the given filename.
        /// </summary>
        /// <param name="fileName">The filename to get a key for.</param>
        /// <returns>A cryptographic key.</returns>
        Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName);
        string CreateRandomPassword();
    }
}