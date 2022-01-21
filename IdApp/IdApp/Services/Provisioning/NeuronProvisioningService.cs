using IdApp.Services.Neuron;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Provisioning;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Provisioning
{
	[Singleton]
	internal sealed class NeuronProvisioningService : ServiceReferences, INeuronProvisioningService
	{
		private ProvisioningClient provisioningClient;

		internal NeuronProvisioningService()
		{
		}

		public ProvisioningClient ProvisioningClient
		{
			get
			{
				if (this.provisioningClient is null || this.provisioningClient.Client != this.NeuronService.Xmpp)
				{
					this.provisioningClient = (this.NeuronService as NeuronService)?.ProvisioningClient;
					if (this.provisioningClient is null)
						throw new InvalidOperationException(AppResources.ProvisioningServiceNotFound);
				}

				return this.provisioningClient;
			}
		}

		/// <summary>
		/// JID of provisioning service.
		/// </summary>
		public string ServiceJid => this.ProvisioningClient.ProvisioningServerAddress;

		/// <summary>
		/// Gets a (partial) list of my devices.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <returns>Found devices, and if there are more devices available.</returns>
		public Task<(SearchResultThing[], bool)> GetMyDevices(int Offset, int MaxCount)
		{
			TaskCompletionSource<(SearchResultThing[], bool)> Result = new TaskCompletionSource<(SearchResultThing[], bool)>();

			this.ProvisioningClient.GetDevices(Offset, MaxCount, (sender, e) =>
			{
				if (e.Ok)
					Result.TrySetResult((e.Things, e.More));
				else
					Result.TrySetException(e.StanzaError ?? new Exception(AppResources.UnableToGetListOfMyDevices));

				return Task.CompletedTask;
			}, null);

			return Result.Task;
		}

		/// <summary>
		/// Gets the full list of my devices.
		/// </summary>
		/// <returns>Complete list of my devices.</returns>
		public async Task<SearchResultThing[]> GetAllMyDevices()
		{
			(SearchResultThing[] Things, bool More) = await this.GetMyDevices(0, 100);
			if (!More)
				return Things;

			List<SearchResultThing> Result = new List<SearchResultThing>();
			int Offset = Things.Length;

			Result.AddRange(Things);

			while (More)
			{
				(Things, More) = await this.GetMyDevices(Offset, 100);
				Result.AddRange(Things);
				Offset += Things.Length;
			}

			return Result.ToArray();
		}

	}
}