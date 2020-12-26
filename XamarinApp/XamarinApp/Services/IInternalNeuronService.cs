using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;

namespace XamarinApp.Services
{
    public interface IInternalNeuronService : INeuronService
    {
        Task<ContractsClient> CreateContractsClientAsync();
        Task<HttpFileUploadClient> CreateFileUploadClientAsync();
        Task UnloadFast();
    }
}