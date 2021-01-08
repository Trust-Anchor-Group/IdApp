using System;
using System.Threading.Tasks;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    internal class NetworkService : INetworkService
    {
        private const int DefaultXmppPortNumber = 5222;
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged; 

        public NetworkService(ILogService logService, IUiDispatcher uiDispatcher)
        {
            this.uiDispatcher = uiDispatcher;
            this.logService = logService;
            if (DeviceInfo.Platform != DevicePlatform.Unknown && !DesignMode.IsDesignModeEnabled) // Need to check this, as Xamarin.Essentials doesn't work in unit tests. It has no effect when running on a real phone.
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

        public async Task<bool> Request(Func<Task> func, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func();
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1>(Func<TIn1, Task> func, TIn1 p1, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2>(Func<TIn1, TIn2, Task> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3>(Func<TIn1, TIn2, TIn3, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4>(Func<TIn1, TIn2, TIn3, TIn4, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8, p9);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true)
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
                return true;
            }, rethrowException);
            return succeeded;
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TReturn>(Func<TIn1, Task<TReturn>> func, TIn1 p1, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TReturn>(Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TReturn>(Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8, p9), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10), rethrowException);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true)
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11), rethrowException);
        }

        private async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequestInner<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true)
        {
            Exception thrownException;
            try
            {
                if (!this.IsOnline)
                {
                    thrownException = new MissingNetworkException(AppResources.ThereIsNoNetwork);
                    logService.LogException(thrownException);
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.ThereIsNoNetwork);
                    }
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
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestTimedOut);
                    }
                }
                else if (ae.InnerException is TaskCanceledException tce)
                {
                    logService.LogException(tce);
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestWasCancelled);
                    }
                }
                else if (ae.InnerException != null)
                {
                    logService.LogException(ae.InnerException);
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ae.InnerException.Message);
                    }
                }
                else
                {
                    logService.LogException(ae);
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ae.Message);
                    }
                }
            }
            catch (TimeoutException te)
            {
                thrownException = te;
                logService.LogException(te);
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestTimedOut);
                }
            }
            catch (TaskCanceledException tce)
            {
                thrownException = tce;
                logService.LogException(tce);
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.RequestWasCancelled);
                }
            }
            catch (Exception e)
            {
                thrownException = e;
                logService.LogException(e);
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, e.Message);
                }
            }

            if (rethrowException)
            {
                throw thrownException;
            }
            return (false, default);
        }
    }
}