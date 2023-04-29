using EDaler;
using IdApp.Resx;
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
				  Svgs.QrCodePerson, 230, 230, string.Empty)
		{
		}
	}
}
