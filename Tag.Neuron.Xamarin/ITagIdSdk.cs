using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;

namespace Tag.Neuron.Xamarin
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup(bool isResuming);
        Task Shutdown(bool keepRunningInTheBackground);
        Task ShutdownInPanic();
        void AutoSave();
        ITagProfile TagProfile { get; }
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