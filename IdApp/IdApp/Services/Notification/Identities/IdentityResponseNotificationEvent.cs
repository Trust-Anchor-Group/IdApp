using IdApp.Pages.Identity.ViewIdentity;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Notification event for identity responses.
	/// </summary>
	public class IdentityResponseNotificationEvent : IdentityNotificationEvent
	{
		/// <summary>
		/// Notification event for identity responses.
		/// </summary>
		public IdentityResponseNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for identity responses.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public IdentityResponseNotificationEvent(LegalIdentityPetitionResponseEventArgs e)
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
			LegalIdentity Identity = this.Identity;

			if (!this.Response || Identity is null)
				await ServiceReferences.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Message"], LocalizationResourceManager.Current["SignaturePetitionDenied"], LocalizationResourceManager.Current["Ok"]);
			else
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewIdentityPage),
					new ViewIdentityNavigationArgs(Identity));
			}
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetCategoryDescription(ServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;
			StringBuilder Result = new();

			Result.Append(LocalizationResourceManager.Current["IdentityResponse"]);

			if (Identity is not null)
			{
				Result.Append(": ");
				Result.Append(ContactInfo.GetFriendlyName(Identity));
			}

			Result.Append('.');

			return Task.FromResult(Result.ToString());
		}
	}
}
