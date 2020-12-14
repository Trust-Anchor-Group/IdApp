using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace XamarinApp.Services
{
    public interface INetworkService : IDisposable
    {
        event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
        Task<(string hostName, int port)> GetXmppHostnameAndPort(string domainName);
        bool IsOnline { get; }
        Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TReturn>(ILogService logService, INavigationService navigationService, Func<Task<TReturn>> func);
        Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TIn, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn, Task<TReturn>> func, TIn p1);
        Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TIn1, TIn2, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2);
        Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TIn1, TIn2, TIn3, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3);
    }
}