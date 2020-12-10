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

    }
}