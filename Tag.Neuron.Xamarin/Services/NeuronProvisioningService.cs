using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class NeuronProvisioningService : INeuronProvisioningService
	{
		private readonly INeuronService neuronService;
		private ProvisioningClient provisioningClient;

		internal NeuronProvisioningService(INeuronService neuronService)
		{
			this.neuronService = neuronService;
		}

		public ProvisioningClient ProvisioningClient
		{
			get
			{
				if (this.provisioningClient is null || this.provisioningClient.Client != this.neuronService.Xmpp)
				{
					this.provisioningClient = (this.neuronService as NeuronService)?.ProvisioningClient;
					if (this.provisioningClient is null)
						throw new InvalidOperationException(AppResources.ProvisioningServiceNotFound);
				}

				return this.provisioningClient;
			}
		}
	}
}