using IdApp.Services;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;

namespace IdApp.Links
{
	/// <summary>
	/// Default link opener.
	/// </summary>
	public class DefaultLink : ILinkOpener
	{
		/// <summary>
		/// Default link opener.
		/// </summary>
		public DefaultLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Grade.Barely;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			if (await Launcher.TryOpenAsync(Link.OriginalString))
				return true;
			else
			{
				ServiceReferences Services = new();

				await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
					LocalizationResourceManager.Current["QrCodeNotUnderstood"] + Environment.NewLine +
					Environment.NewLine + Link.OriginalString);

				return false;
			}
		}
	}
}
