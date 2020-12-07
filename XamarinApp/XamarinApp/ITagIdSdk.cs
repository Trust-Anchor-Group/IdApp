using System;
using System.Threading.Tasks;
using XamarinApp.Services;

namespace XamarinApp
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup();
        Task Shutdown();
        void AutoSave();
        TagProfile TagProfile { get; }
        IAuthService AuthService { get; }
        INeuronService NeuronService { get; }
        INetworkService NetworkService { get; }
        ILogService LogService { get; }
        ISettingsService SettingsService { get; }
    }
}