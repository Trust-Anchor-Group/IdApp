using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;

namespace Tag.Sdk.Core.Services
{
    public interface IInternalNeuronService : INeuronService
    {
        Task<ContractsClient> CreateContractsClientAsync();
        Task<HttpFileUploadClient> CreateFileUploadClientAsync();
        Task UnloadFast();
    }
}