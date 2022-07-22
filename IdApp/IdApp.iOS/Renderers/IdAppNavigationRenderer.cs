using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NavigationPage), typeof(IdApp.iOS.Renderers.IdAppNavigationRenderer))]
namespace IdApp.iOS.Renderers
{
	public class IdAppNavigationRenderer : NavigationRenderer
	{
		public override void ViewWillAppear(bool animated)
		{
			// https://github.com/xamarin/Xamarin.Forms/issues/4343
			UIStatusBarStyle StatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;
			base.ViewWillAppear(animated);
			UIApplication.SharedApplication.StatusBarStyle = StatusBarStyle;
		}
	}
}
