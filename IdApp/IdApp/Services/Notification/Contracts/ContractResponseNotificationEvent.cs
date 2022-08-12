using IdApp.Pages.Contracts.ViewContract;
using IdApp.Resx;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for contract petition responses.
	/// </summary>
	public class ContractResponseNotificationEvent : ContractNotificationEvent
	{
		/// <summary>
		/// Notification event for contract petition responses.
		/// </summary>
		public ContractResponseNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for contract petition responses.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public ContractResponseNotificationEvent(ContractPetitionResponseEventArgs e)
			: base(e)
		{
			this.Response = e.Response;
		}

		/// <summary>
		/// Response
		/// </summary>
		public bool Response { get; set; }

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();

			if (!this.Response || Contract is null)
			{
				await ServiceReferences.UiSerializer.DisplayAlert(AppResources.Message,
					AppResources.PetitionToViewContractWasDenied, AppResources.Ok);
			}
			else
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewContractPage),
					new ViewContractNavigationArgs(Contract, false));
			}
		}
	}
}
