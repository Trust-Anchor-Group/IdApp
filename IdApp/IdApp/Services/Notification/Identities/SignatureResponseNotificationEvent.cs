using IdApp.Pages.Identity.ViewIdentity;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Notification event for signature responses.
	/// </summary>
	public class SignatureResponseNotificationEvent : IdentityNotificationEvent
	{
		/// <summary>
		/// Notification event for signature responses.
		/// </summary>
		public SignatureResponseNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for signature responses.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public SignatureResponseNotificationEvent(SignaturePetitionResponseEventArgs e)
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
		public override async Task Open(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (!this.Response || Identity is null)
				await ServiceReferences.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Message"], LocalizationResourceManager.Current["PetitionToViewLegalIdentityWasDenied"], LocalizationResourceManager.Current["Ok"]);
			else
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;
			StringBuilder Result = new();

			Result.Append(LocalizationResourceManager.Current["SignatureResponse"]);

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
