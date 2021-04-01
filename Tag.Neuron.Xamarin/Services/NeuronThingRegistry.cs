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
		private readonly INeuronService neuronService;
		private ThingRegistryClient registryClient;

		internal NeuronThingRegistry(INeuronService neuronService)
		{
			this.neuronService = neuronService;
		}

		public ThingRegistryClient RegistryClient
		{
			get
			{
				if (this.registryClient is null || this.registryClient.Client != this.neuronService.Xmpp)
				{
					this.registryClient = (this.neuronService as NeuronService)?.ThingRegistryClient;
					if (this.registryClient is null)
						throw new InvalidOperationException(AppResources.ThingRegistryServiceNotFound);
				}

				return this.registryClient;
			}
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

		public Task<NodeResultEventArgs> ClaimThing(string DiscoUri, bool MakePublic)
		{
			if (!this.TryDecodeIoTDiscoClaimURI(DiscoUri, out MetaDataTag[] Tags))
				throw new ArgumentException(AppResources.InvalidIoTDiscoClaimUri, nameof(DiscoUri));

			TaskCompletionSource<NodeResultEventArgs> Result = new TaskCompletionSource<NodeResultEventArgs>();

			this.RegistryClient.Mine(MakePublic, Tags, (sender, e) =>
			{
				Result.TrySetResult(e);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		public Task<bool> Disown(string RegistryJid, string ThingJid, string SourceId, string Partition, string NodeId)
		{
			TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();

			this.RegistryClient.Disown(RegistryJid, ThingJid, NodeId, SourceId, Partition, (sender, e) =>
			{
				Result.TrySetResult(e.Ok);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}
	}
}