using System;
using System.Globalization;

namespace IdApp.Models
{
    /// <summary>
    /// The data model for a contract.
    /// </summary>
    public class ContractModel
    {
        /// <summary>
        /// Creates an instance of the <see cref="ContractModel"/> class.
        /// </summary>
        /// <param name="contractId">The contract id.</param>
        /// <param name="timestamp">The timestamp to show with the contract reference.</param>
        /// <param name="name">The name of the contract.</param>
        public ContractModel(string contractId, DateTime timestamp, string name)
        {
            this.ContractId = contractId;
            this.Timestamp = timestamp.ToString(CultureInfo.CurrentUICulture);
            this.Name = name;
        }

        /// <summary>
        /// The contract id.
        /// </summary>
        public string ContractId { get; }

        /// <summary>
        /// The created timestamp of the contract.
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// The name of the contract.
        /// </summary>
        public string Name { get; }
    }
}