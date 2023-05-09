using IdApp.Services;
using IdApp.Services.Notification.Identities;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Links
{
	/// <summary>
	/// Opens Legal Identity links.
	/// </summary>
	public class LegalIdentityLink : ILinkOpener
	{
		/// <summary>
		/// Opens Legal Identity links.
		/// </summary>
		public LegalIdentityLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.ToLower() == Constants.UriSchemes.UriSchemeIotId ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			ServiceReferences Services = new();
			Services.NotificationService.ExpectEvent<IdentityResponseNotificationEvent>(DateTime.Now.AddMinutes(1));

			string LegalId = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			await Services.ContractOrchestratorService.OpenLegalIdentity(LegalId, LocalizationResourceManager.Current["ScannedQrCode"]);

			return true;
		}
	}
}
