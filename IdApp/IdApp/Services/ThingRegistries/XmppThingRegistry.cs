using IdApp.Resx;
using IdApp.Services.Xmpp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;
using Waher.Runtime.Inventory;

namespace IdApp.Services.ThingRegistries
{
	[Singleton]
	internal sealed class XmppThingRegistry : ServiceReferences, IXmppThingRegistry
	{
		private ThingRegistryClient registryClient;

		internal XmppThingRegistry()
		{
		}

		public ThingRegistryClient RegistryClient
		{
			get
			{
				if (this.registryClient is null || this.registryClient.Client != this.XmppService.Xmpp)
				{
					this.registryClient = (this.XmppService as XmppService)?.ThingRegistryClient;
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

		public bool IsIoTDiscoDirectURI(string DiscoUri)
		{
			return ThingRegistryClient.IsIoTDiscoDirectURI(DiscoUri);
		}

		public bool TryDecodeIoTDiscoClaimURI(string DiscoUri, out MetaDataTag[] Tags)
		{
			return ThingRegistryClient.TryDecodeIoTDiscoClaimURI(DiscoUri, out Tags);
		}

		public bool TryDecodeIoTDiscoSearchURI(string DiscoUri, out SearchOperator[] Operators, out string RegistryJid)
		{
			RegistryJid = null;
			Operators = null;
			if (!ThingRegistryClient.TryDecodeIoTDiscoURI(DiscoUri, out IEnumerable<SearchOperator> Operators2))
				return false;

			List<SearchOperator> List = new();

			foreach (SearchOperator Operator in Operators2)
			{
				if (Operator.Name.ToUpper() == "R")
				{
					if (!string.IsNullOrEmpty(RegistryJid))
						return false;

					if (Operator is not StringTagEqualTo StrEqOp)
						return false;

					RegistryJid = StrEqOp.Value;
				}
				else
					List.Add(Operator);
			}

			Operators = List.ToArray();
			return true;
		}

		public bool TryDecodeIoTDiscoDirectURI(string DiscoUri, out string Jid, out string SourceId, out string NodeId, 
			out string PartitionId, out MetaDataTag[] Tags)
		{
			Jid = null;
			SourceId = null;
			NodeId = null;
			PartitionId = null;

			if (!ThingRegistryClient.TryDecodeIoTDiscoURI(DiscoUri, out IEnumerable<SearchOperator> Operators2))
			{
				Tags = null;
				return false;
			}

			List<MetaDataTag> TagsFound = new();

			foreach (SearchOperator Operator in Operators2)
			{
				if (Operator is StringTagEqualTo S)
				{
					switch (S.Name.ToUpper())
					{
						case "JID":
							Jid = S.Value;
							break;

						case "SID":
							SourceId = S.Value;
							break;

						case "NID":
							NodeId = S.Value;
							break;

						case "PT":
							PartitionId = S.Value;
							break;

						default:
							TagsFound.Add(new MetaDataStringTag(S.Name, S.Value));
							break;
					}
				}
				else if (Operator is NumericTagEqualTo N)
					TagsFound.Add(new MetaDataNumericTag(N.Name, N.Value));
				else
				{
					Tags = null;
					return false;
				}
			}

			Tags = TagsFound.ToArray();
			return !string.IsNullOrEmpty(Jid);
		}


		public Task<NodeResultEventArgs> ClaimThing(string DiscoUri, bool MakePublic)
		{
			if (!this.TryDecodeIoTDiscoClaimURI(DiscoUri, out MetaDataTag[] Tags))
				throw new ArgumentException(AppResources.InvalidIoTDiscoClaimUri, nameof(DiscoUri));

			TaskCompletionSource<NodeResultEventArgs> Result = new();

			this.RegistryClient.Mine(MakePublic, Tags, (sender, e) =>
			{
				Result.TrySetResult(e);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		public Task<bool> Disown(string RegistryJid, string ThingJid, string SourceId, string Partition, string NodeId)
		{
			TaskCompletionSource<bool> Result = new();

			this.RegistryClient.Disown(RegistryJid, ThingJid, NodeId, SourceId, Partition, (sender, e) =>
			{
				Result.TrySetResult(e.Ok);
				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Searches for devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <param name="DiscoUri">iotdisco URI.</param>
		/// <returns>Devices found, Registry JID, and if more devices are available.</returns>
		public async Task<(SearchResultThing[], string, bool)> Search(int Offset, int MaxCount, string DiscoUri)
		{
			if (!this.TryDecodeIoTDiscoSearchURI(DiscoUri, out SearchOperator[] Operators, out string RegistryJid))
				return (new SearchResultThing[0], RegistryJid, false);

			(SearchResultThing[] Things, bool More) = await this.Search(Offset, MaxCount, RegistryJid, Operators);

			return (Things, RegistryJid, More);
		}

		/// <summary>
		/// Searches for devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>Devices found, and if more devices are available.</returns>
		public Task<(SearchResultThing[], bool)> Search(int Offset, int MaxCount, string RegistryJid, params SearchOperator[] Operators)
		{
			TaskCompletionSource<(SearchResultThing[], bool)> Result = new();

			this.RegistryClient.Search(RegistryJid, Offset, MaxCount, Operators, (sender, e) =>
			{
				if (e.Ok)
					Result.TrySetResult((e.Things, e.More));
				else
					Result.TrySetException(e.StanzaError ?? new Exception("Unable to perform search."));

				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Searches for all devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="DiscoUri">iotdisco URI.</param>
		/// <returns>Complete list of devices in registry matching the search operators, and the JID of the registry service.</returns>
		public async Task<(SearchResultThing[], string)> SearchAll(string DiscoUri)
		{
			if (!this.TryDecodeIoTDiscoSearchURI(DiscoUri, out SearchOperator[] Operators, out string RegistryJid))
				return (new SearchResultThing[0], RegistryJid);

			SearchResultThing[] Things = await this.SearchAll(RegistryJid, Operators);

			return (Things, RegistryJid);
		}

		/// <summary>
		/// Searches for all devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>Complete list of devices in registry matching the search operators.</returns>
		public async Task<SearchResultThing[]> SearchAll(string RegistryJid, params SearchOperator[] Operators)
		{
			(SearchResultThing[] Things, bool More) = await this.Search(0, Constants.BatchSizes.DeviceBatchSize, RegistryJid, Operators);
			if (!More)
				return Things;

			List<SearchResultThing> Result = new();
			int Offset = Things.Length;

			Result.AddRange(Things);

			while (More)
			{
				(Things, More) = await this.Search(Offset, Constants.BatchSizes.DeviceBatchSize, RegistryJid, Operators);
				Result.AddRange(Things);
				Offset += Things.Length;
			}

			return Result.ToArray();
		}

	}
}