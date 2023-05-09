using IdApp.Services;
using IdApp.Services.Notification.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Links
{
	/// <summary>
	/// Opens Smart Contract links.
	/// </summary>
	public class SmartContractLink : ILinkOpener
	{
		/// <summary>
		/// Opens Smart Contract links.
		/// </summary>
		public SmartContractLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.ToLower() == Constants.UriSchemes.UriSchemeIotSc ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			ServiceReferences Services = new();
			Services.NotificationService.ExpectEvent<ContractResponseNotificationEvent>(DateTime.Now.AddMinutes(1));

			Dictionary<CaseInsensitiveString, object> Parameters = new();

			string contractId = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			int i = contractId.IndexOf('?');

			if (i > 0)
			{
				NameValueCollection QueryParameters = HttpUtility.ParseQueryString(contractId[i..]);

				foreach (string Key in QueryParameters.AllKeys)
					Parameters[Key] = QueryParameters[Key];

				contractId = contractId[..i];
			}

			await Services.ContractOrchestratorService.OpenContract(contractId, LocalizationResourceManager.Current["ScannedQrCode"], Parameters);
			return true;
		}
	}
}
