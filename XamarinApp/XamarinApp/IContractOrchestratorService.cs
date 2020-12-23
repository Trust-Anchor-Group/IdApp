using System.Threading.Tasks;
using XamarinApp.Services;

namespace XamarinApp
{
    /// <summary>
    /// Orchestrates operations on contracts upon receiving certain events, like approving or rejecting other peers' review requests.
    /// </summary>
    public interface IContractOrchestratorService : ILoadableService
    {
        Task OpenLegalIdentity(string legalId, string purpose);
        Task OpenContract(string contractId, string purpose);
    }
}