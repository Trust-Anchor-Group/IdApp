using System.Threading.Tasks;
using XamarinApp.Services;

namespace XamarinApp
{
    public interface IIdentityOrchestratorService : ILoadableService
    {
        Task OpenLegalIdentity(string legalId, string purpose);
        Task OpenContract(string contractId, string purpose);
    }
}