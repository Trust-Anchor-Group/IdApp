using System;
using System.Threading.Tasks;
using XamarinApp.Services;

namespace XamarinApp
{
    public interface ITagIdSdk : IDisposable
    {
        Task Startup(IAuthService authService);
        Task Shutdown();
    }
}