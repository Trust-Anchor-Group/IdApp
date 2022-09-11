using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.Concentrator;

namespace IdApp.Services.IoT
{
    /// <summary>
    /// Adds support for XMPP Provisioning
    /// </summary>
    [DefaultImplementation(typeof(IoTService))]
    public interface IIoTService
	{
		/// <summary>
		/// Access to provisioning client, for authorization control
		/// </summary>
		ProvisioningClient ProvisioningClient { get; }

		/// <summary>
		/// Access to sensor client, for sensor data readout and subscription
		/// </summary>
		SensorClient SensorClient { get; }

		/// <summary>
		/// Access to control client, for access to actuators
		/// </summary>
		ControlClient ControlClient { get; }

		/// <summary>
		/// Access to concentrator client, for administrative purposes of concentrators
		/// </summary>
		ConcentratorClient ConcentratorClient { get; }

		/// <summary>
		/// JID of provisioning service.
		/// </summary>
		string ServiceJid
		{
            get;
		}

        /// <summary>
        /// Gets a (partial) list of my devices.
        /// </summary>
        /// <param name="Offset">Start offset of list</param>
        /// <param name="MaxCount">Maximum number of items in response.</param>
        /// <returns>Found devices, and if there are more devices available.</returns>
        Task<(SearchResultThing[], bool)> GetMyDevices(int Offset, int MaxCount);

        /// <summary>
        /// Gets the full list of my devices.
        /// </summary>
        /// <returns>Complete list of my devices.</returns>
        Task<SearchResultThing[]> GetAllMyDevices();

    }
}
