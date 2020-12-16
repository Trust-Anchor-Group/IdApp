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
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TReturn>(ILogService logService, INavigationService navigationService, Func<Task<TReturn>> func, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn, Task<TReturn>> func, TIn p1, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false);
    }
}