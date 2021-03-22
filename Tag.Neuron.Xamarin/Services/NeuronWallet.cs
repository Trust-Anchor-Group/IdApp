using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using Waher.Runtime.Inventory;

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
		/// Sends an eDaler URI to the eDaler service.
		/// </summary>
		/// <param name="Uri">eDaler URI</param>
		/// <returns>Transaction object containing information about the processed URI.</returns>
		public Task<Transaction> SendUri(string Uri)
		{
			return this.eDalerClient.SendEDalerUriAsync(Uri);
		}

	}
}