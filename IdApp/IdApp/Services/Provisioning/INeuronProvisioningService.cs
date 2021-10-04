using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Networking.XMPP.Provisioning;

namespace IdApp.Services.Provisioning
{
    /// <summary>
    /// Adds support for XMPP Provisioning
    /// </summary>
    [DefaultImplementation(typeof(NeuronProvisioningService))]
    public interface INeuronProvisioningService
	{
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