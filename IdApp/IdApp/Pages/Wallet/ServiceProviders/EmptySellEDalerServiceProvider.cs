using EDaler;
using IdApp.Resx;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Empty service provider. Used to show the caller the user wants to request a service from a user.
	/// </summary>
	internal class EmptySellEDalerServiceProvider : SellEDalerServiceProvider
	{
		public EmptySellEDalerServiceProvider()
			: base(string.Empty, string.Empty, LocalizationResourceManager.Current["ToUser2"],
				  Svgs.QrCodePerson, 230, 230, string.Empty)
		{
		}
	}
}
