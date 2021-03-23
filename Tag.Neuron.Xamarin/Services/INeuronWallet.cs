using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for eDaler wallets.
    /// </summary>
    [DefaultImplementation(typeof(NeuronWallet))]
    public interface INeuronWallet
    {
		/// <summary>
		/// Tries to parse an eDaler URI.
		/// </summary>
		/// <param name="Uri">URI string.</param>
		/// <param name="Parsed">Parsed eDaler URI, if successful.</param>
		/// <returns>If URI string could be parsed.</returns>
		bool TryParseEDalerUri(string Uri, out EDalerUri Parsed);

		/// <summary>
		/// Tries to decrypt an encrypted private message.
		/// </summary>
		/// <param name="EncryptedMessage">Encrypted message.</param>
		/// <param name="PublicKey">Public key used.</param>
		/// <param name="RemoteEndpoint">Remote endpoint</param>
		/// <returns>Decrypted string, if successful, or null, if not.</returns>
		Task<string> TryDecryptMessage(byte[] EncryptedMessage, byte[] PublicKey, string RemoteEndpoint);

		/// <summary>
		/// Sends an eDaler URI to the eDaler service.
		/// </summary>
		/// <param name="Uri">eDaler URI</param>
		/// <returns>Transaction object containing information about the processed URI.</returns>
		Task<Transaction> SendUri(string Uri);
	}
}