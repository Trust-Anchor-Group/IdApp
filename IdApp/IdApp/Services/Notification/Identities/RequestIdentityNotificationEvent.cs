using IdApp.Pages.Identity.PetitionIdentity;
using IdApp.Resx;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Notification event for identity petitions.
	/// </summary>
	public class RequestIdentityNotificationEvent : IdentityNotificationEvent
	{
		/// <summary>
		/// Notification event for identity petitions.
		/// </summary>
		public RequestIdentityNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for identity petitions.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public RequestIdentityNotificationEvent(LegalIdentityPetitionEventArgs e)
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
				await ServiceReferences.NavigationService.GoToAsync(nameof(PetitionIdentityPage), new PetitionIdentityNavigationArgs(
					this.Identity, this.RequestorFullJid, this.SignatoryIdentityId, this.PetitionId, this.Purpose));
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

			Result.Append(AppResources.RequestToAccessIdentity);

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
