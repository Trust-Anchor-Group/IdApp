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
				  string.Empty, true, "https://upload.wikimedia.org/wikipedia/en/6/62/Kermit_the_Frog.jpg", 282, 353)
		{
		}
	}
}
