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
		private readonly IInternalNeuronService neuronService;
		private ThingRegistryClient registryClient;

		internal NeuronThingRegistry(IInternalNeuronService neuronService)
		{
			this.neuronService = neuronService;
		}

		public ThingRegistryClient RegistryClient
		{
			get
			{
				if (this.registryClient is null)
					this.registryClient = this.neuronService.CreateThingRegistryClient();

				return this.registryClient;
			}
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

		public Task<string> ClaimThing(string DiscoUri, bool MakePublic)
		{
			if (!this.TryDecodeIoTDiscoClaimURI(DiscoUri, out MetaDataTag[] Tags))
				throw new ArgumentException(AppResources.InvalidIoTDiscoClaimUri, nameof(DiscoUri));

			TaskCompletionSource<string> Result = new TaskCompletionSource<string>();

			this.RegistryClient.Mine(MakePublic, Tags, (sender, e) =>
			{
				if (e.Ok)
					Result.TrySetResult(null);
				else
					Result.TrySetResult(string.IsNullOrEmpty(e.ErrorText) ? AppResources.UnableToClaimThing : e.ErrorText);

				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

	}
}