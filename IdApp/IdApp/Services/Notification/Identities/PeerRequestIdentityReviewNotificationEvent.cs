using IdApp.Pages.Identity.ViewIdentity;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Notification event for peer reviews of identities.
	/// </summary>
	public class PeerRequestIdentityReviewNotificationEvent : IdentityNotificationEvent
	{
		/// <summary>
		/// Notification event for peer reviews of identities.
		/// </summary>
		public PeerRequestIdentityReviewNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for contract proposals.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public PeerRequestIdentityReviewNotificationEvent(SignaturePetitionEventArgs e)
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
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity,
					this.RequestorFullJid, this.SignatoryIdentityId, this.PetitionId, this.Purpose, this.ContentToSign));
			}
		}
	}
}
