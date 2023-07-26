using Foundation;
using IdApp.Links;
using IdApp.Services;
using System.Threading.Tasks;
using UIKit;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace IdApp.iOS.Links
{
	/// <summary>
	/// Opens Bank-ID links
	/// </summary>
	public class BankIdLinks : ILinkOpener
	{
		/// <summary>
		/// Opens Bank-ID links
		/// </summary>
		public BankIdLinks()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(System.Uri Link)
		{
			return Link.OriginalString.StartsWith("https://app.bankid.com", System.StringComparison.CurrentCultureIgnoreCase) ||
				Link.Scheme.ToLower() == "bankid" ? Grade.Excellent : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public Task<bool> TryOpenLink(System.Uri Link)
		{
			try
			{
				// https://www.bankid.com/utvecklare/guider/teknisk-integrationsguide/programstart

				NSUrl Url = new(Link.OriginalString);
				UIApplicationOpenUrlOptions options = new() { UniversalLinksOnly = true };

				new ServiceReferences().UiSerializer.BeginInvokeOnMainThread(() =>
				{
					UIApplication.SharedApplication.OpenUrl(Url, options, null);
				});

				return Task.FromResult(true);
			}
			catch (System.Exception ex)
			{
				Log.Critical(ex);
				return Task.FromResult(false);
			}
		}
	}
}
