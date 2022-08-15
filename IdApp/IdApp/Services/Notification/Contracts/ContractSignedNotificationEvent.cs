using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Resx;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for when a contract has been signed.
	/// </summary>
	public class ContractSignedNotificationEvent : ContractNotificationEvent
	{
		/// <summary>
		/// Notification event for when a contract has been signed.
		/// </summary>
		public ContractSignedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for when a contract has been signed.
		/// </summary>
		/// <param name="Contract">Requested contract.</param>
		/// <param name="e">Event arguments.</param>
		public ContractSignedNotificationEvent(Contract Contract, ContractReferenceEventArgs e)
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

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetCategoryDescription(ServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();
			StringBuilder Result = new();

			Result.Append(AppResources.ContractSignatureReceived);

			if (Contract is not null)
			{
				Result.Append(": ");
				Result.Append(await ContractModel.GetCategory(Contract));
			}

			Result.Append('.');

			return Result.ToString();
		}
	}
}
