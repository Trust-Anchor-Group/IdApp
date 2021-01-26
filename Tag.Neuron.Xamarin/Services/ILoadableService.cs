using System;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    public interface ILoadableService
    {
        Task Load(bool isResuming);
        Task Unload();
        event EventHandler<LoadedEventArgs> Loaded;
    }
}