using System.Collections.Generic;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
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
        /// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
        /// </summary>
        /// <param name="contractTypesPerCategory">A dictionary of all contract types, organized per category.</param>
        public NewContractNavigationArgs(SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
        {
            this.Contract = null;
            this.ContractTypesPerCategory = contractTypesPerCategory;
        }

        /// <summary>
        /// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
        /// </summary>
        /// <param name="contract">The contract to use as template.</param>
        /// <param name="contractTypesPerCategory">A dictionary of all contract types, organized per category.</param>
        public NewContractNavigationArgs(Contract contract, SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
        {
            this.Contract = contract;
            this.ContractTypesPerCategory = contractTypesPerCategory;
        }

        /// <summary>
        /// The contract to use as template.
        /// </summary>
        public Contract Contract { get; }

        /// <summary>
        /// The contract types available, organized per category.
        /// </summary>
        public SortedDictionary<string, SortedDictionary<string, string>> ContractTypesPerCategory { get; }
    }
}