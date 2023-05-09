using IdApp.Services;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Links
{
	/// <summary>
	/// Opens Things Discovery links.
	/// </summary>
	public class ThingsDiscoveryLink : ILinkOpener
	{
		/// <summary>
		/// Opens Things Discovery links.
		/// </summary>
		public ThingsDiscoveryLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.ToLower() == Constants.UriSchemes.UriSchemeIotDisco ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			ServiceReferences Services = new();
			string Url = Link.OriginalString;

			if (Services.XmppService.IsIoTDiscoClaimURI(Url))
				await Services.ThingRegistryOrchestratorService.OpenClaimDevice(Url);
			else if (Services.XmppService.IsIoTDiscoSearchURI(Url))
				await Services.ThingRegistryOrchestratorService.OpenSearchDevices(Url);
			else if (Services.XmppService.IsIoTDiscoDirectURI(Url))
				await Services.ThingRegistryOrchestratorService.OpenDeviceReference(Url);
			else
			{
				await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["InvalidIoTDiscoveryCode"] + Environment.NewLine + Environment.NewLine + Url);
				return false;
			}

			return true;
		}
	}
}
