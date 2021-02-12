using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a server signature.
    /// </summary>
    public class ServerSignatureNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ServerSignatureNavigationArgs"/> class.
        /// </summary>
        /// <param name="contract">The contract to display.</param>
        public ServerSignatureNavigationArgs(Contract contract)
        {
            this.Contract = contract;
        }

        /// <summary>
        /// The contract to display.
        /// </summary>
        public Contract Contract { get; }
    }
}