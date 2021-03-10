using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class NeuronThingRegistry : INeuronThingRegistry
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiDispatcher uiDispatcher;
		private readonly IInternalNeuronService neuronService;
		private readonly ILogService logService;
		private ThingRegistryClient registryClient;

		internal NeuronThingRegistry(ITagProfile tagProfile, IUiDispatcher uiDispatcher, IInternalNeuronService neuronService, ILogService logService)
		{
			this.tagProfile = tagProfile;
			this.uiDispatcher = uiDispatcher;
			this.neuronService = neuronService;
			this.logService = logService;
		}

		internal async Task CreateClient()
		{
			if (!string.IsNullOrWhiteSpace(this.tagProfile.RegistryJid))
				this.registryClient = await this.neuronService.CreateThingRegistryClientAsync();
		}

		internal void DestroyClient()
		{
			this.registryClient?.Dispose();
			this.registryClient = null;
		}

		public bool IsIoTDiscoClaimURI(string DiscoUri)
		{
			return ThingRegistryClient.IsIoTDiscoClaimURI(DiscoUri);
		}

		public bool IsIoTDiscoSearchURI(string DiscoUri)
		{
			return ThingRegistryClient.IsIoTDiscoSearchURI(DiscoUri);
		}

		public bool TryDecodeIoTDiscoClaimURI(string DiscoUri, out MetaDataTag[] Tags)
		{
			return ThingRegistryClient.TryDecodeIoTDiscoClaimURI(DiscoUri, out Tags);
		}

		public bool TryDecodeIoTDiscoSearchURI(string DiscoUri, out IEnumerable<SearchOperator> Operators)
		{
			return ThingRegistryClient.TryDecodeIoTDiscoURI(DiscoUri, out Operators);
		}

	}
}