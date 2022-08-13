using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Resx;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

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
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (!this.Response || Identity is null)
				await ServiceReferences.UiSerializer.DisplayAlert(AppResources.Message, AppResources.PetitionToViewLegalIdentityWasDenied, AppResources.Ok);
			else
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
		}
	}
}
