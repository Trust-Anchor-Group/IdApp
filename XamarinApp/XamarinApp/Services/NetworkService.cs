﻿using System;
using System.Threading.Tasks;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Xamarin.Essentials;

namespace XamarinApp.Services
{
    internal class NetworkService : INetworkService
    {
        private const int DefaultXmppPortNumber = 5222;
        private readonly ILogService logService;

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged; 

        public NetworkService(ILogService logService)
        {
            this.logService = logService;
            if (DeviceInfo.Platform != DevicePlatform.Unknown) // Need to check this, as Xamarin.Essentials doesn't work in unit tests. It has no effect when running on a real phone.
            {
                Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            }
        }

        public void Dispose()
        {
            if (DeviceInfo.Platform != DevicePlatform.Unknown)
            {
                Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
            }
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.ConnectivityChanged?.Invoke(this, e);
        }

        public virtual bool IsOnline => Connectivity.NetworkAccess == NetworkAccess.Internet ||
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

        public async Task<bool> Request(INavigationService navigationService, Func<Task> func, bool rethrowException = false)
        {
            (bool succeeded, bool _) = await PerformRequestInner<bool>(navigationService, async () =>
            {
                await func();
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1>(INavigationService navigationService, Func<TIn1, Task> func, TIn1 p1, bool rethrowException = false)
        {
            (bool succeeded, bool _) = await PerformRequestInner<bool>(navigationService, async () =>
            {
                await func(p1);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2>(INavigationService navigationService, Func<TIn1, TIn2, Task> func, TIn1 p1, TIn2 p2, bool rethrowException = false)
        {
            (bool succeeded, bool _) = await PerformRequestInner<bool>(navigationService, async () =>
            {
                await func(p1, p2);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false)
        {
            (bool succeeded, bool _) = await PerformRequestInner<bool>(navigationService, async () =>
            {
                await func(p1, p2, p3);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false)
        {
            (bool succeeded, bool _) = await PerformRequestInner<bool>(navigationService, async () =>
            {
                await func(p1, p2, p3, p4);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TReturn>(INavigationService navigationService, Func<Task<TReturn>> func, bool rethrowException = false)
        {
            return PerformRequestInner(navigationService, async () => await func(), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TReturn>(INavigationService navigationService, Func<TIn1, Task<TReturn>> func, TIn1 p1, bool rethrowException = false)
        {
            return PerformRequestInner(navigationService, async () => await func(p1), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false)
        {
            return PerformRequestInner(navigationService, async () => await func(p1, p2), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false)
        {
            return PerformRequestInner(navigationService, async () => await func(p1, p2, p3), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TReturn>(INavigationService navigationService, Func<TIn1, TIn2, TIn3, TIn4, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false)
        {
            return PerformRequestInner(navigationService, async () => await func(p1, p2, p3, p4), rethrowException);
        }

        private async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequestInner<TReturn>(INavigationService navigationService, Func<Task<TReturn>> func, bool rethrowException = false)
        {
            Exception thrownException;
            try
            {
                if (!this.IsOnline)
                {
                    thrownException = new MissingNetworkException(AppResources.ThereIsNoNetwork);
                    logService.LogException(thrownException);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.ThereIsNoNetwork);
                }
                else
                {
                    TReturn t = await func();
                    return (true, t);
                }
            }
            catch (AggregateException ae)
            {
                thrownException = ae;
                if (ae.InnerException is TimeoutException te)
                {
                    logService.LogException(te);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestTimedOut);
                }
                else if (ae.InnerException is TaskCanceledException tce)
                {
                    logService.LogException(tce);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestWasCancelled);
                }
                else if (ae.InnerException != null)
                {
                    logService.LogException(ae.InnerException);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, ae.InnerException.Message);
                }
                else
                {
                    logService.LogException(ae);
                    await navigationService.DisplayAlert(AppResources.ErrorTitle, ae.Message);
                }
            }
            catch (TimeoutException te)
            {
                thrownException = te;
                logService.LogException(te);
                await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestTimedOut);
            }
            catch (TaskCanceledException tce)
            {
                thrownException = tce;
                logService.LogException(tce);
                await navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestWasCancelled);
            }
            catch (Exception e)
            {
                thrownException = e;
                logService.LogException(e);
                await navigationService.DisplayAlert(AppResources.ErrorTitle, e.Message);
            }

            if (rethrowException)
            {
                throw thrownException;
            }
            return (false, default);
        }
    }
}