using System;
using System.Threading.Tasks;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;

namespace Tag.Sdk.Core
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup(Application application);
        Task Shutdown();
        Task ShutdownInPanic();
        void AutoSave();
        TagProfile TagProfile { get; }
        IAuthService AuthService { get; }
        INeuronService NeuronService { get; }
        INetworkService NetworkService { get; }
        ILogService LogService { get; }
        IStorageService StorageService { get; }
        ISettingsService SettingsService { get; }
        INavigationService NavigationService { get; }
    }
}