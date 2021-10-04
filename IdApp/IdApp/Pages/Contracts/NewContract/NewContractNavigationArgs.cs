using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Contracts.NewContract
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