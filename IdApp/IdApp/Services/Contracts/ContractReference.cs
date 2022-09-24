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
	[Index("IsTemplate", "ContractLoaded", "Category", "Name", "Created")]
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
		/// When object was last loaded.
		/// </summary>
		public DateTime Loaded { get; set; }

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
		/// If a local copy of the contract is available.
		/// </summary>
		public bool ContractLoaded { get; set; }

		/// <summary>
		/// If the contract can work as a template
		/// </summary>
		public bool IsTemplate { get; set; }

		/// <summary>
		/// Contract state
		/// </summary>
		public ContractState State { get; set; }

		/// <summary>
		/// Gets a parsed contract.
		/// </summary>
		/// <returns>Parsed contract</returns>
		public async Task<Contract> GetContract()
		{
			if (this.contract is null && !string.IsNullOrEmpty(this.ContractXml))
			{
				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(this.ContractXml);

				ParsedContract Parsed = await Contract.Parse(Doc.DocumentElement);

				this.contract = Parsed?.Contract;
			}

			return this.contract;
		}

		/// <summary>
		/// Sets a parsed contract.
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <param name="Services">Service references.</param>
		public async Task SetContract(Contract Contract, IServiceReferences Services)
		{
			this.contract = Contract;

			if (Contract is null)
			{
				this.ContractXml = null;
				this.ContractLoaded = false;
				this.IsTemplate = false;
				this.Name = string.Empty;
				this.Category = string.Empty;
				this.State = ContractState.Failed;
			}
			else
			{
				StringBuilder Xml = new();
				Contract.Serialize(Xml, true, true, true, true, true, true, true);
				this.ContractXml = Xml.ToString();
				this.ContractLoaded = true;
				this.ContractId = Contract.ContractId;
				this.IsTemplate = Contract.PartsMode == ContractParts.TemplateOnly;
				this.State = Contract.State;
				this.Created = Contract.Created;
				this.Updated = Contract.Updated;
				this.Loaded = DateTime.UtcNow;

				this.Name = await ContractModel.GetName(Contract, Services);
				this.Category = await ContractModel.GetCategory(Contract);
			}
		}
	}
}
