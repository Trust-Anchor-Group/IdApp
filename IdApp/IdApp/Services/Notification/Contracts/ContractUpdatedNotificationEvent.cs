using IdApp.Pages.Contracts.ViewContract;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for when a contract has been updated.
	/// </summary>
	public class ContractUpdatedNotificationEvent : ContractNotificationEvent
	{
		/// <summary>
		/// Notification event for when a contract has been updated.
		/// </summary>
		public ContractUpdatedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for when a contract has been updated.
		/// </summary>
		/// <param name="Contract">Requested contract.</param>
		/// <param name="e">Event arguments.</param>
		public ContractUpdatedNotificationEvent(Contract Contract, ContractReferenceEventArgs e)
			: base(Contract, e)
		{
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();

			await ServiceReferences.NavigationService.GoToAsync(nameof(ViewContractPage),
				new ViewContractNavigationArgs(Contract, false));
		}
	}
}
