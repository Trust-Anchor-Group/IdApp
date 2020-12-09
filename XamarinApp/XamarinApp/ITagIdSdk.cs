using System;
using System.Threading.Tasks;
using XamarinApp.Services;

namespace XamarinApp
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup();
        Task Shutdown();
        Task ShutdownInPanic();
        void AutoSave();
        TagProfile TagProfile { get; }
        IAuthService AuthService { get; }
        INeuronService NeuronService { get; }
        IContractsService ContractsService { get; }
        INetworkService NetworkService { get; }
        ILogService LogService { get; }
        IStorageService StorageService { get; }
        ISettingsService SettingsService { get; }
    }
}