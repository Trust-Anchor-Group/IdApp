using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Registration.ValidateIdentity
{
	/// <summary>
	/// Represents representing a peer review from a peer, by scanning their QR-code.
	/// </summary>
	public class RequestFromPeer : ServiceProviderWithLegalId
	{
		/// <summary>
		/// Represents representing a peer review from a peer, by scanning their QR-code.
		/// </summary>
		public RequestFromPeer()
			: base(string.Empty,string.Empty, LocalizationResourceManager.Current["RequestReviewFromAPeer"],
				  string.Empty, true, "https://lab.tagroot.io/Community/Images/2022/12/26/Kermit%20%287%29.png", 920, 845)
		{
		}
	}
}
