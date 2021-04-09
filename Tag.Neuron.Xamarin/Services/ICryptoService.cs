using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Cryptographic service that helps create passwords and other security related tasks.
    /// </summary>
    [DefaultImplementation(typeof(CryptoService))]
    public interface ICryptoService
    {
        /// <summary>
        /// Returns a cryptographic authorization key for the given filename.
        /// </summary>
        /// <param name="fileName">The filename to get a key for.</param>
        /// <returns>A cryptographic key.</returns>
        Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName);

        /// <summary>
        /// Generates a random password to use.
        /// </summary>
        /// <returns></returns>
        string CreateRandomPassword();
    }
}