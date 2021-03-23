using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using Waher.Events;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class NeuronWallet : INeuronWallet
	{
		private readonly IInternalNeuronService neuronService;
		private EDalerClient eDalerClient;

		internal NeuronWallet(IInternalNeuronService neuronService)
		{
			this.neuronService = neuronService;
		}

		public EDalerClient EDalerClient
		{
			get
			{
				if (this.eDalerClient is null)
					this.eDalerClient = this.neuronService.CreateEDalerClient();

				return this.eDalerClient;
			}
		}

		internal void DestroyClient()
		{
			this.eDalerClient?.Dispose();
			this.eDalerClient = null;
		}

		/// <summary>
		/// Tries to parse an eDaler URI.
		/// </summary>
		/// <param name="Uri">URI string.</param>
		/// <param name="Parsed">Parsed eDaler URI, if successful.</param>
		/// <returns>If URI string could be parsed.</returns>
		public bool TryParseEDalerUri(string Uri, out EDalerUri Parsed)
		{
			return EDalerUri.TryParse(Uri, out Parsed);
		}

		/// <summary>
		/// Tries to decrypt an encrypted private message.
		/// </summary>
		/// <param name="EncryptedMessage">Encrypted message.</param>
		/// <param name="PublicKey">Public key used.</param>
		/// <param name="RemoteEndpoint">Remote endpoint</param>
		/// <returns>Decrypted string, if successful, or null, if not.</returns>
		public async Task<string> TryDecryptMessage(byte[] EncryptedMessage, byte[] PublicKey, string RemoteEndpoint)
		{
			try
			{
				// TODO: Replace with method overload from EDaler v1.0.3:
				if (!string.IsNullOrEmpty(RemoteEndpoint) && !(PublicKey is null))
				{
					string RemotePublicKey = await RuntimeSettings.GetAsync("EDaler.Remote.Key." + RemoteEndpoint, string.Empty);

					if (!string.IsNullOrEmpty(RemotePublicKey))
						PublicKey = Convert.FromBase64String(RemotePublicKey);
				}

				return await this.eDalerClient.DecryptMessage(EncryptedMessage, PublicKey);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
				return string.Empty;
			}
		}

		/// <summary>
		/// Sends an eDaler URI to the eDaler service.
		/// </summary>
		/// <param name="Uri">eDaler URI</param>
		/// <returns>Transaction object containing information about the processed URI.</returns>
		public Task<Transaction> SendUri(string Uri)
		{
			return this.EDalerClient.SendEDalerUriAsync(Uri);
		}

	}
}