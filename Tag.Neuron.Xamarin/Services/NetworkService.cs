using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal class NetworkService : LoadableService, INetworkService
    {
        private const int DefaultXmppPortNumber = 5222;
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged; 

        public NetworkService(ILogService logService, IUiDispatcher uiDispatcher)
        {
            this.uiDispatcher = uiDispatcher;
            this.logService = logService;
        }

        ///<inheritdoc/>
        public override Task Load(bool isResuming)
        {
            if (this.BeginLoad())
            {
                if (DeviceInfo.Platform != DevicePlatform.Unknown && !DesignMode.IsDesignModeEnabled) // Need to check this, as Xamarin.Essentials doesn't work in unit tests. It has no effect when running on a real phone.
                    Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
                
                this.EndLoad(true);
            }

            return Task.CompletedTask;
        }

        ///<inheritdoc/>
        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                if (DeviceInfo.Platform != DevicePlatform.Unknown)
                    Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
                
                this.EndUnload();
            }

            return Task.CompletedTask;
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.ConnectivityChanged?.Invoke(this, e);
        }

        public virtual bool IsOnline => 
            Connectivity.NetworkAccess == NetworkAccess.Internet ||
            Connectivity.NetworkAccess == NetworkAccess.ConstrainedInternet;

        public async Task<(string hostName, int port, bool isIpAddress)> LookupXmppHostnameAndPort(string domainName)
        {
            if (IPAddress.TryParse(domainName, out IPAddress _))
                return (domainName, DefaultXmppPortNumber, true);

            try
            {
                SRV endpoint = await DnsResolver.LookupServiceEndpoint(domainName, "xmpp-client", "tcp");
                if (!(endpoint is null) && !string.IsNullOrWhiteSpace(endpoint.TargetHost) && endpoint.Port > 0)
                    return (endpoint.TargetHost, endpoint.Port, false);
            }
            catch (Exception)
            {
                // No service endpoint registered
            }

            return (domainName, DefaultXmppPortNumber, false);
        }

        public async Task<bool> TryRequest(Func<Task> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            (bool succeeded, bool _) = await PerformRequestInner(async () =>
            {
                await func();
                return true;
            }, memberName, rethrowException, displayAlert);

            return succeeded;
        }

        public Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TReturn>(Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "")
        {
            return PerformRequestInner(async () => await func(), memberName, rethrowException, displayAlert);
        }

        private async Task<(bool Succeeded, TReturn ReturnValue)> PerformRequestInner<TReturn>(Func<Task<TReturn>> func, string memberName, bool rethrowException = false, bool displayAlert = true)
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
                    TReturn t = await func().TimeoutAfter(Constants.Timeouts.GenericRequest);
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
                else if (!(ae.InnerException is null))
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
                string message;
                if(e is XmppException xe && !(xe.Stanza is null))
                {
                    message = xe.Stanza.InnerText;
                }
                else
                {
                    message = e.Message;
                }
                logService.LogException(e, GetParameter(memberName));
                if (displayAlert)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, CreateMessage(message, memberName));
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
#if DEBUG
            if (!string.IsNullOrWhiteSpace(memberName))
            {
                return $"{message}{Environment.NewLine}Caller: {memberName}";
            }
#endif
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