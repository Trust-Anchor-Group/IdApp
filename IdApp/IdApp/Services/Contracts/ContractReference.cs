using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;

namespace IdApp.Services.Contracts
{
	/// <summary>
	/// Contains a local reference to a contract that the user has created or signed.
	/// </summary>
	[CollectionName("ContractReferences")]
	[Index("ContractId")]
	[Index("Category", "Name", "Created")]
	public class ContractReference
	{
		private Contract contract;

		/// <summary>
		/// Contains a local reference to a contract that the user has created or signed.
		/// </summary>
		public ContractReference()
		{
		}

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string ObjectId { get; set; }

		/// <summary>
		/// Contract reference
		/// </summary>
		public string ContractId { get; set; }

		/// <summary>
		/// When object was created
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// When object was last updated.
		/// </summary>
		public DateTime Updated { get; set; }

		/// <summary>
		/// XML of contract.
		/// </summary>
		public string ContractXml { get; set; }

		/// <summary>
		/// Name of contract
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Category of contract
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Gets a parsed contract.
		/// </summary>
		/// <returns>Parsed contract</returns>
		public async Task<Contract> GetContract()
		{
			if (this.contract is null && !string.IsNullOrEmpty(this.ContractXml))
			{
				XmlDocument Doc = new();
				Doc.LoadXml(this.ContractXml);

				ParsedContract Parsed = await Contract.Parse(Doc);

				this.contract = Parsed.Contract;
			}

			return this.contract;
		}

		/// <summary>
		/// Sets a parsed contract.
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <param name="Services">Service references.</param>
		public async Task SetContract(Contract Contract, ServiceReferences Services)
		{
			this.contract = Contract;

			if (Contract is null)
				this.ContractXml = null;
			else
			{
				StringBuilder Xml = new();
				Contract.Serialize(Xml, true, true, true, true, true, true, true);
				this.ContractXml = Xml.ToString();
			}

			this.Name = await ContractModel.GetName(Contract, Services);
			this.Category = await ContractModel.GetCategory(Contract);
		}
	}
}
