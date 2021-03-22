using System.Threading.Tasks;
using EDaler;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.Provisioning;

namespace Tag.Neuron.Xamarin.Services
{
    internal interface IInternalNeuronService : INeuronService
    {
        Task<ContractsClient> CreateContractsClientAsync(bool CanCreateKeys);
        HttpFileUploadClient CreateFileUploadClient();
        MultiUserChatClient CreateMultiUserChatClient();
        ThingRegistryClient CreateThingRegistryClient();
        EDalerClient CreateEDalerClient();
    }
}