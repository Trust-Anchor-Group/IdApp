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

        Task<bool> Request(INavigationService navigationService, Func<Task> func, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1>(INavigationService navigationService, Func<TIn1, Task> func, TIn1 p1, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2>(INavigationService navigationService, Func<TIn1, TIn2, Task> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true);
        Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true);

        Task<(bool Succeeded, TReturn ReturnValue)> Request<TReturn>(INavigationService navigationService, Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn, TReturn>(INavigationService navigationService, Func<TIn, Task<TReturn>> func, TIn p1, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true);
        Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true);
    }
}