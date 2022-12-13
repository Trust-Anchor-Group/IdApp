using IdApp.Services.Xmpp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.IoT
{
	[Singleton]
	internal sealed class IoTService : ServiceReferences, IIoTService
	{
		private ProvisioningClient provisioningClient;
		private SensorClient sensorClient;
		private ControlClient controlClient;
		private ConcentratorClient concentratorClient;

		internal IoTService()
		{
		}

		/// <summary>
		/// Access to provisioning client, for authorization control
		/// </summary>
		public ProvisioningClient ProvisioningClient
		{
			get
			{
				if (this.provisioningClient is null || this.provisioningClient.Client != this.XmppService.Xmpp)
				{
					this.provisioningClient = (this.XmppService as XmppService)?.ProvisioningClient;
					if (this.provisioningClient is null)
						throw new InvalidOperationException(LocalizationResourceManager.Current["ProvisioningServiceNotFound"]);
				}

				return this.provisioningClient;
			}
		}

		/// <summary>
		/// Access to sensor client, for sensor data readout and subscription
		/// </summary>
		public SensorClient SensorClient
		{
			get
			{
				if (this.sensorClient is null || this.sensorClient.Client != this.XmppService.Xmpp)
				{
					this.sensorClient = (this.XmppService as XmppService)?.SensorClient;
					if (this.sensorClient is null)
						throw new InvalidOperationException(LocalizationResourceManager.Current["SensorServiceNotFound"]);
				}

				return this.sensorClient;
			}
		}

		/// <summary>
		/// Access to control client, for access to actuators
		/// </summary>
		public ControlClient ControlClient
		{
			get
			{
				if (this.controlClient is null || this.controlClient.Client != this.XmppService.Xmpp)
				{
					this.controlClient = (this.XmppService as XmppService)?.ControlClient;
					if (this.controlClient is null)
						throw new InvalidOperationException(LocalizationResourceManager.Current["ControlServiceNotFound"]);
				}

				return this.controlClient;
			}
		}

		/// <summary>
		/// Access to concentrator client, for administrative purposes of concentrators
		/// </summary>
		public ConcentratorClient ConcentratorClient
		{
			get
			{
				if (this.concentratorClient is null || this.concentratorClient.Client != this.XmppService.Xmpp)
				{
					this.concentratorClient = (this.XmppService as XmppService)?.ConcentratorClient;
					if (this.concentratorClient is null)
						throw new InvalidOperationException(LocalizationResourceManager.Current["ConcentratorServiceNotFound"]);
				}

				return this.concentratorClient;
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
			TaskCompletionSource<(SearchResultThing[], bool)> Result = new();

			this.ProvisioningClient.GetDevices(Offset, MaxCount, (sender, e) =>
			{
				if (e.Ok)
					Result.TrySetResult((e.Things, e.More));
				else
					Result.TrySetException(e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetListOfMyDevices"]));

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
			(SearchResultThing[] Things, bool More) = await this.GetMyDevices(0, Constants.BatchSizes.DeviceBatchSize);
			if (!More)
				return Things;

			List<SearchResultThing> Result = new();
			int Offset = Things.Length;

			Result.AddRange(Things);

			while (More)
			{
				(Things, More) = await this.GetMyDevices(Offset, Constants.BatchSizes.DeviceBatchSize);
				Result.AddRange(Things);
				Offset += Things.Length;
			}

			return Result.ToArray();
		}

	}
}
