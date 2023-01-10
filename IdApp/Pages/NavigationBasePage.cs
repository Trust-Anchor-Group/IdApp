using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using XF = Xamarin.Forms;

namespace IdApp.Pages
{
	/// <summary>
	/// A base class for all navigation pages.
	/// </summary>
	public class NavigationBasePage : XF.NavigationPage
	{
		/// <summary>
		/// Creates an instance of the <see cref="NavigationBasePage"/> class.
		/// </summary>
		public NavigationBasePage() : base()
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NavigationBasePage"/> class.
		/// </summary>
		/// <param name="Root">The starting page.</param>
		public NavigationBasePage(XF.Page Root) : base(Root)
		{
			this.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
			this.On<iOS>().DisableTranslucentNavigationBar();
			this.On<iOS>().SetHideNavigationBarSeparator(true);
		}
	}
}
