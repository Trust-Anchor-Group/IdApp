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
    }
}