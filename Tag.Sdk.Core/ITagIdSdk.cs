using System;
using System.Threading.Tasks;
using Tag.Sdk.Core.Services;

namespace Tag.Sdk.Core
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup(bool isResuming);
        Task Shutdown(bool keepRunningInTheBackground);
        Task ShutdownInPanic();
        void AutoSave();
        TagProfile TagProfile { get; }
        IUiDispatcher UiDispatcher { get; }
        IAuthService AuthService { get; }
        INeuronService NeuronService { get; }
        INetworkService NetworkService { get; }
        ILogService LogService { get; }
        IStorageService StorageService { get; }
        ISettingsService SettingsService { get; }
        INavigationService NavigationService { get; }
    }
}