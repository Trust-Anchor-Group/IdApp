using System;
using System.Threading.Tasks;
using XamarinApp.Services;

namespace XamarinApp
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup();
        Task Shutdown();
        TagProfile TagProfile { get; }
        IAuthService AuthService { get; }
        INeuronService NeuronService { get; }
        INetworkService NetworkService { get; }
        ILogService LogService { get; }
    }
}