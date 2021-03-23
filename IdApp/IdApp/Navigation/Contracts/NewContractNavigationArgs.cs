using System.Collections.Generic;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation.Contracts
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a new contract.
    /// </summary>
    public class NewContractNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
        /// </summary>
        /// <param name="contract">The contract to use as template.</param>
        public NewContractNavigationArgs(Contract contract)
        {
            this.Contract = contract;
        }

        /// <summary>
        /// The contract to use as template.
        /// </summary>
        public Contract Contract { get; }
    }
}