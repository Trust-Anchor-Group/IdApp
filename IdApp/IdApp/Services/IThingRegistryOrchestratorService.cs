using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    /// <summary>
    /// Orchestrates operations on things via Thing Registry services.
    /// </summary>
    [DefaultImplementation(typeof(ThingRegistryOrchestratorService))]
    public interface IThingRegistryOrchestratorService : ILoadableService
    {
        /// <summary>
        /// Opens the Claim Device View, allowing the user to revise device information found in the iotdisco URI provided,
        /// and claim it.
        /// </summary>
        /// <param name="Uri">iotdisco claim URI</param>
        Task OpenClaimDevice(string Uri);

        /// <summary>
        /// Opens the Search Devices View, allowing the user to revise search parameters found in the iotdisco URI provided,
        /// and start the search for devices.
        /// </summary>
        /// <param name="Uri">iotdisco search URI</param>
        Task OpenSearchDevices(string Uri);
    }
}