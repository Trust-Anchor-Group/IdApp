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

        Task<bool> Request(INavigationService navigationService, Func<Task> func, bool rethrowException = false);
        Task<bool> Request<TIn1>(INavigationService navigationService, Func<TIn1, Task> func, TIn1 p1, bool rethrowException = false);
        Task<bool> Request<TIn1, TIn2>(INavigationService navigationService, Func<TIn1, TIn2, Task> func, TIn1 p1, TIn2 p2, bool rethrowException = false);
        Task<bool> Request<TIn1, TIn2, TIn3>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false);

        Task<(bool Succeeded, TReturn ReturnValue)> Request<TReturn>(INavigationService navigationService, Func<Task<TReturn>> func, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn, TReturn>(INavigationService navigationService, Func<TIn, Task<TReturn>> func, TIn p1, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false);
    }
}