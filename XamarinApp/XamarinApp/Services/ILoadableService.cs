using System;
using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface ILoadableService
    {
        Task Load();
        Task Unload();
        event EventHandler<LoadedEventArgs> Loaded;
    }
}