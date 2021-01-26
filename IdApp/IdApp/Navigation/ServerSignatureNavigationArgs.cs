using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
{
    public class ServerSignatureNavigationArgs : NavigationArgs
    {
        public ServerSignatureNavigationArgs(Contract contract)
        {
            this.Contract = contract;
        }

        public Contract Contract { get; }
    }
}