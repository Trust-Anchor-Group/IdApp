using System;
using EDaler;
using EDaler.Uris;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class NeuronWallet : INeuronWallet
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiDispatcher uiDispatcher;
		private readonly IInternalNeuronService neuronService;
		private readonly ILogService logService;
		private readonly INeuronContracts contracts;
		private EDalerClient eDalerClient;

		internal NeuronWallet(ITagProfile tagProfile, IUiDispatcher uiDispatcher, IInternalNeuronService neuronService, 
			ILogService logService, INeuronContracts contracts)
		{
			this.tagProfile = tagProfile;
			this.uiDispatcher = uiDispatcher;
			this.neuronService = neuronService;
			this.logService = logService;
			this.contracts = contracts;
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

	}
}