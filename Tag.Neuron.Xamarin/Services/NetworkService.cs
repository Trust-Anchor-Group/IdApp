using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
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

        public async Task<(string hostName, int port)> LookupXmppHostnameAndPort(string domainName)
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

        public async Task<bool> TryRequest(Func<Task> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func();
                return true;
            }, rethrowException, displayAlert, memberName);
            return succeeded;
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(), rethrowException, displayAlert, memberName);
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