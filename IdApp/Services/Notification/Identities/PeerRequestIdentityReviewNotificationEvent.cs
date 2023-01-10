using IdApp.Pages.Identity.ViewIdentity;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

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
		public override async Task Open(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (Identity is not null)
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity,
					this.RequestorFullJid, this.SignatoryIdentityId, this.PetitionId, this.Purpose, this.ContentToSign));
			}
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;
			StringBuilder Result = new();

			Result.Append(LocalizationResourceManager.Current["IdentityReviewRequest"]);

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
