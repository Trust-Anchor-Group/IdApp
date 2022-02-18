using IdApp.Services.Contracts;
using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModel
{
	/// <summary>
	/// Contract reference.
	/// </summary>
	public class ContractReference
	{
		private readonly string contractId;
		private Contract contract;
		private DateTime timestamp = DateTime.MinValue;

		/// <summary>
		/// Contract reference.
		/// </summary>
		/// <param name="Timestamp">Timestamp</param>
		/// <param name="ContractId">Contract ID</param>
		public ContractReference(DateTime Timestamp, string ContractId)
		{
			this.contractId = ContractId;
			this.contract = null;
			this.timestamp = Timestamp;
		}

		/// <summary>
		/// Contract reference.
		/// </summary>
		/// <param name="Contract">Contract</param>
		public ContractReference(Contract Contract)
		{
			this.contract = Contract;
			this.contractId = Contract.ContractId;
			this.timestamp = Contract.Created;
		}

		/// <summary>
		/// Contract ID
		/// </summary>
		public string ContractId => this.contractId;

		/// <summary>
		/// Timestamp
		/// </summary>
		public DateTime Timestamp
		{
			get => this.timestamp;
			set => this.timestamp = value;
		}

		/// <summary>
		/// Gets the contract
		/// </summary>
		/// <param name="Contracts">Contracts.</param>
		/// <returns>Contract reference.</returns>
		public async Task<Contract> GetContract(ISmartContracts Contracts)
		{
			if (this.contract is null)
			{
				this.contract = await Contracts.ContractsClient.GetContractAsync(this.contractId);
				if (this.timestamp == DateTime.MinValue && !(this.contract is null))
					this.timestamp = this.contract.Created;
			}

			return this.contract;
		}
	}
}
