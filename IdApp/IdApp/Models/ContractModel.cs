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
        /// <param name="created">The created timestamp of the contract.</param>
        /// <param name="name">The name of the contract.</param>
        public ContractModel(string contractId, DateTime created, string name)
        {
            this.ContractId = contractId;
            this.Created = created.ToString(CultureInfo.CurrentUICulture);
            this.Name = name;
        }
        /// <summary>
        /// The contract id.
        /// </summary>
        public string ContractId { get; }
        /// <summary>
        /// The created timestamp of the contract.
        /// </summary>
        public string Created { get; }
        /// <summary>
        /// The name of the contract.
        /// </summary>
        public string Name { get; }
    }
}