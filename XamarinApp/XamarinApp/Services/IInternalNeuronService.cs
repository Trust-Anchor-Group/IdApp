using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface IInternalNeuronService : INeuronService
    {
        Task UnloadFast();
    }
}