using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation.Contracts
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a contract.
    /// </summary>
    public class ViewContractNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="ViewContractNavigationArgs"/> class.
        /// </summary>
        /// <param name="contract">The contract to display.</param>
        /// <param name="isReadOnly"><c>true</c> if the contract is readonly, <c>false</c> otherwise.</param>
        public ViewContractNavigationArgs(Contract contract, bool isReadOnly)
        {
            this.Contract = contract;
            this.IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// The contract to display.
        /// </summary>
        public Contract Contract { get; }

        /// <summary>
        /// <c>true</c> if the contract is readonly, <c>false</c> otherwise.
        /// </summary>
        public bool IsReadOnly { get; }
    }
}