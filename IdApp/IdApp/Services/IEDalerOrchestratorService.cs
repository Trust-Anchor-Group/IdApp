using System.Threading.Tasks;
using IdApp.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    /// <summary>
    /// Orchestrates eDaler operations.
    /// </summary>
    [DefaultImplementation(typeof(EDalerOrchestratorService))]
    public interface IEDalerOrchestratorService : ILoadableService
    {
        /// <summary>
        /// Opens the wallet
        /// </summary>
        Task OpenWallet();

        /// <summary>
        /// eDaler URI scanned.
        /// </summary>
        /// <param name="uri">eDaler URI.</param>
        Task OpenEDalerUri(string uri);
    }
}