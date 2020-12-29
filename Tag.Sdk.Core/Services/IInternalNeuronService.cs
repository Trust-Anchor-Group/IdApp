using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;

namespace Tag.Sdk.Core.Services
{
    public interface IInternalNeuronService : INeuronService
    {
        Task<ContractsClient> CreateContractsClientAsync();
        Task<HttpFileUploadClient> CreateFileUploadClientAsync();
        Task<MultiUserChatClient> CreateMultiUserChatClientAsync();
        Task UnloadFast();
    }
}