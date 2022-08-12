using IdApp.Pages.Contracts.PetitionSignature;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Notification event for signature petitions.
	/// </summary>
	public class RequestSignatureNotificationEvent : IdentityNotificationEvent
	{
		/// <summary>
		/// Notification event for signature petitions.
		/// </summary>
		public RequestSignatureNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for signature petitions.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public RequestSignatureNotificationEvent(SignaturePetitionEventArgs e)
			: base(e)
		{
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (Identity is not null)
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(PetitionSignaturePage), new PetitionSignatureNavigationArgs(
					this.Identity, this.RequestorFullJid, this.SignatoryIdentityId, this.ContentToSign, this.PetitionId, this.Purpose));
			}
		}
	}
}
