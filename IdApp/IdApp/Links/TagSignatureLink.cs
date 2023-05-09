using IdApp.Services;
using IdApp.Services.Notification.Identities;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace IdApp.Links
{
	/// <summary>
	/// Opens TAG signature links.
	/// </summary>
	public class TagSignatureLink : ILinkOpener
	{
		/// <summary>
		/// Opens TAG signature links.
		/// </summary>
		public TagSignatureLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.ToLower() == Constants.UriSchemes.UriSchemeTagSign ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			ServiceReferences Services = new();
			Services.NotificationService.ExpectEvent<RequestSignatureNotificationEvent>(DateTime.Now.AddMinutes(1));

			string request = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			await Services.ContractOrchestratorService.TagSignature(request);

			return true;
		}
	}
}
