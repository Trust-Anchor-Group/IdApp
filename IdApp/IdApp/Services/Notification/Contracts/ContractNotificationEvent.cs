using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Resx;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Abstract base class of Contract notification events.
	/// </summary>
	public abstract class ContractNotificationEvent : NotificationEvent
	{
		private Contract contract;

		/// <summary>
		/// Abstract base class of Contract notification events.
		/// </summary>
		public ContractNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class of Contract notification events.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public ContractNotificationEvent(ContractProposalEventArgs e)
			: base()
		{
			this.ContractId = e.ContractId;
			this.Category = e.ContractId;
			this.Button = EventButton.Contracts;
			this.Received = DateTime.UtcNow;
		}

		/// <summary>
		/// Abstract base class of Contract notification events.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public ContractNotificationEvent(ContractPetitionResponseEventArgs e)
			: base()
		{
			this.ContractId = e.RequestedContract?.ContractId ?? string.Empty;
			this.Category = this.ContractId;
			this.Button = EventButton.Contracts;
			this.Received = DateTime.UtcNow;

			this.SetContract(e.RequestedContract);
		}

		/// <summary>
		/// Abstract base class of Contract notification events.
		/// </summary>
		/// <param name="Contract">Requested contract.</param>
		/// <param name="e">Event arguments</param>
		public ContractNotificationEvent(Contract Contract, ContractPetitionEventArgs e)
			: base()
		{
			this.ContractId = Contract.ContractId;
			this.Category = e.RequestedContractId;
			this.Button = EventButton.Contracts;
			this.Received = DateTime.UtcNow;

			this.SetContract(Contract);
		}

		/// <summary>
		/// Abstract base class of Contract notification events.
		/// </summary>
		/// <param name="Contract">Requested contract.</param>
		/// <param name="e">Event arguments</param>
		public ContractNotificationEvent(Contract Contract, ContractReferenceEventArgs e)
			: base()
		{
			this.ContractId = Contract.ContractId;
			this.Category = e.ContractId;
			this.Button = EventButton.Contracts;
			this.Received = DateTime.UtcNow;

			this.SetContract(Contract);
		}

		/// <summary>
		/// Contract ID.
		/// </summary>
		public string ContractId { get; set; }

		/// <summary>
		/// XML of contract.
		/// </summary>
		public string ContractXml { get; set; }

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
		public void SetContract(Contract Contract)
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
		}

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences"></param>
		/// <returns></returns>
		public override Task<string> GetCategoryIcon(ServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.Paragraph);
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetCategoryDescription(ServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();

			if (Contract is null)
				return this.ContractId;
			else
				return await ContractModel.GetCategory(Contract);
		}

	}
}
