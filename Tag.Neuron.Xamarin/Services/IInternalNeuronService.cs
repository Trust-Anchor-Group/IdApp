using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;

namespace Tag.Neuron.Xamarin.Services
{
    internal interface IInternalNeuronService : INeuronService
    {
        Task<ContractsClient> CreateContractsClientAsync();
        Task<HttpFileUploadClient> CreateFileUploadClientAsync();
        Task<MultiUserChatClient> CreateMultiUserChatClientAsync();
    }
}