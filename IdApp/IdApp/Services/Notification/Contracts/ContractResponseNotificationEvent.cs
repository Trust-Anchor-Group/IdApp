using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.ViewContract;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

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
				await ServiceReferences.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Message"],
					LocalizationResourceManager.Current["PetitionToViewContractWasDenied"], LocalizationResourceManager.Current["Ok"]);
			}
			else
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewContractPage),
					new ViewContractNavigationArgs(Contract, false));
			}
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(ServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();
			StringBuilder Result = new();

			Result.Append(LocalizationResourceManager.Current["ContractResponse"]);

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
