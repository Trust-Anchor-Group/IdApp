using System.Threading.Tasks;
using Tag.Sdk.Core.Services;
using XamarinApp.Services;

namespace XamarinApp.Services
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