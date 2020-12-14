using System;
using System.Threading.Tasks;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Xamarin.Essentials;

namespace XamarinApp.Services
{
    internal sealed class NetworkService : INetworkService
    {
        private const int DefaultXmppPortNumber = 5222;

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged; 

        public NetworkService()
        {
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        public void Dispose()
        {
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.ConnectivityChanged?.Invoke(this, e);
        }

        public bool IsOnline => Connectivity.NetworkAccess == NetworkAccess.Internet ||
                                Connectivity.NetworkAccess == NetworkAccess.ConstrainedInternet;

        public async Task<(string hostName, int port)> GetXmppHostnameAndPort(string domainName)
        {
            try
            {
                SRV endpoint = await DnsResolver.LookupServiceEndpoint(domainName, "xmpp-client", "tcp");
                if (!(endpoint is null) && !string.IsNullOrWhiteSpace(endpoint.TargetHost) && endpoint.Port > 0)
                    return (endpoint.TargetHost, endpoint.Port);
            }
            catch (Exception)
            {
                // No service endpoint registered
            }

            return (domainName, DefaultXmppPortNumber);
        }

        public async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TReturn>(ILogService logService, INavigationService navigationService, Func<Task<TReturn>> func)
        {
            return await PerformRequestInner<TReturn>(logService, navigationService, async () => await func());
        }

        public async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TIn1, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, Task<TReturn>> func, TIn1 p1)
        {
            return await PerformRequestInner<TReturn>(logService, navigationService, async () => await func(p1));
        }

        public async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TIn1, TIn2, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2)
        {
            return await PerformRequestInner<TReturn>(logService, navigationService, async () => await func(p1, p2));
        }

        public async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequest<TIn1, TIn2, TIn3, TReturn>(ILogService logService, INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3)
        {
            return await PerformRequestInner<TReturn>(logService, navigationService, async () => await func(p1, p2, p3));
        }

        private async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequestInner<TReturn>(ILogService logService, INavigationService navigationService, Func<Task<TReturn>> func)
        {
            try
            {
                if (!this.IsOnline)
                {
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.ThereIsNoNetwork);
                    return default;
                }

                TReturn t = await func();
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is TimeoutException te)
                {
                    logService.LogException(te);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestTimedOut);
                }
                else
                {
                    logService.LogException(ae);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, ae.Message);
                }
            }
            catch (TimeoutException te)
            {
                logService.LogException(te);
                await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestTimedOut);
            }
            catch (Exception e)
            {
                logService.LogException(e);
                await navigationService.DisplayAlert(AppResources.ErrorTitle, e.Message);
            }

            return (false, default);
        }
    }
}