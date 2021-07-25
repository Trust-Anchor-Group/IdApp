using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Contracts.NewContract.ObjectModel
{
    /// <summary>
    /// The data model for contract visibility.
    /// </summary>
    public class ContractVisibilityModel
    {
        /// <summary>
        /// Create an instance of the <see cref="ContractVisibilityModel"/> class.
        /// </summary>
        /// <param name="visibility">The contract visibility.</param>
        /// <param name="name">The contract name.</param>
        public ContractVisibilityModel(ContractVisibility visibility, string name)
        {
            this.Visibility = visibility;
            this.Name = name;
        }

        /// <summary>
        /// The contract visibility.
        /// </summary>
        public ContractVisibility Visibility { get; }
        /// <summary>
        /// The contract name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the string representation, i.e. name, of this <see cref="ContractVisibilityModel"/>.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}