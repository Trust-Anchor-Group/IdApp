using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        public async Task<bool> Request(Func<Task> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func();
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1>(Func<TIn1, Task> func, TIn1 p1, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2>(Func<TIn1, TIn2, Task> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3>(Func<TIn1, TIn2, TIn3, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4>(Func<TIn1, TIn2, TIn3, TIn4, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8, p9);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public async Task<bool> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TReturn>(Func<TIn1, Task<TReturn>> func, TIn1 p1, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TReturn>(Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TReturn>(Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8, p9), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10), rethrowException, displayAlert, memberName);
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> Request<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11), rethrowException, displayAlert, memberName);
        }

        private async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequestInner<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            Exception thrownException;
            try
            {
                if (!this.IsOnline)
                {
                    thrownException = new MissingNetworkException(AppResources.ThereIsNoNetwork);
                    logService.LogException(thrownException, GetParameter(memberName));
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(AppResources.ThereIsNoNetwork, memberName));
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
                    logService.LogException(te, GetParameter(memberName));
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(AppResources.RequestTimedOut, memberName));
                    }
                }
                else if (ae.InnerException is TaskCanceledException tce)
                {
                    logService.LogException(tce, GetParameter(memberName));
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(AppResources.RequestWasCancelled, memberName));
                    }
                }
                else if (ae.InnerException != null)
                {
                    logService.LogException(ae.InnerException, GetParameter(memberName));
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(ae.InnerException.Message, memberName));
                    }
                }
                else
                {
                    logService.LogException(ae, GetParameter(memberName));
                    if (displayAlert)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(ae.Message, memberName));
                    }
                }
            }
            catch (TimeoutException te)
            {
                thrownException = te;
                logService.LogException(te, GetParameter(memberName));
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(AppResources.RequestTimedOut, memberName));
                }
            }
            catch (TaskCanceledException tce)
            {
                thrownException = tce;
                logService.LogException(tce, GetParameter(memberName));
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(AppResources.RequestWasCancelled, memberName));
                }
            }
            catch (Exception e)
            {
                thrownException = e;
                logService.LogException(e, GetParameter(memberName));
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(e.Message, memberName));
                }
            }

            if (rethrowException)
            {
                throw thrownException;
            }
            return (false, default);
        }


        private static string CreateMessage(string message, string memberName)
        {
            if (!string.IsNullOrWhiteSpace(memberName))
            {
                return $"{message}{Environment.NewLine}Caller: {memberName}";
            }

            return message;
        }

        private static KeyValuePair<string, string>[] GetParameter(string memberName)
        {
            if (!string.IsNullOrWhiteSpace(memberName))
            {
                return new []
                {
                    new KeyValuePair<string, string>("Caller", memberName)
                };
            }

            return new KeyValuePair<string, string>[0];
        }
    }
}