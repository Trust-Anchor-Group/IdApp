using EDaler;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Empty service provider. Used to show the caller the user wants to request a service from a user.
	/// </summary>
	internal class EmptyBuyEDalerServiceProvider : BuyEDalerServiceProvider
	{
		public EmptyBuyEDalerServiceProvider()
			: base(string.Empty, string.Empty, LocalizationResourceManager.Current["FromUser"],
				  "https://lab.tagroot.io/Community/Images/2022/12/26/Kermit%20%287%29.png", 920, 845, string.Empty)
		{
		}
	}
}
