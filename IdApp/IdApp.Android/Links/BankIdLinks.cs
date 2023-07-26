using Android.App;
using Android.Content;
using Android.Net;
using IdApp.Links;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace IdApp.Android.Links
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

				Intent Intent = new();
				Intent.SetAction(Intent.ActionView);
				Intent.SetData(Uri.Parse(Link.OriginalString));
				Intent.SetFlags(ActivityFlags.NewTask);

				Application.Context.StartActivity(Intent);

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
