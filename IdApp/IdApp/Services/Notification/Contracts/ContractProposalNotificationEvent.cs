using IdApp.Pages.Contracts.ViewContract;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for contract proposals.
	/// </summary>
	public class ContractProposalNotificationEvent : ContractNotificationEvent
	{
		/// <summary>
		/// Notification event for contract proposals.
		/// </summary>
		public ContractProposalNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for contract proposals.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public ContractProposalNotificationEvent(ContractProposalEventArgs e)
			: base(e)
		{
			this.Role = e.Role;
			this.Message = e.MessageText;
		}

		/// <summary>
		/// Role
		/// </summary>
		public string Role { get; set; }

		/// <summary>
		/// Message
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();

			if (Contract is not null)
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewContractPage),
					new ViewContractNavigationArgs(Contract, false, this.Role, this.Message) { ReturnCounter = 1 });
			}
		}
	}
}
