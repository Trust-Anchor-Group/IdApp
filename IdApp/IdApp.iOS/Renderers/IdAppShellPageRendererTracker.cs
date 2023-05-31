using UIKit;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms.Platform.iOS;

namespace IdApp.iOS.Renderers
{
    internal class IdAppShellPageRendererTracker : ShellPageRendererTracker
    {
        public IdAppShellPageRendererTracker(IShellContext Context) : base(Context)
        {
        }

        protected override void OnRendererSet()
        {
            base.OnRendererSet();

            if (this.ViewController.NavigationItem is UINavigationItem NavigationItem)
            {
                // Shell has BackButtonBehavior, which allows specifying a custom back button title. However, the Shell implementation
                // will remove the iOS back arrow from the back button if a custom back button title is provided. This code preserves
                // the back arrow.
                NavigationItem.BackButtonTitle = LocalizationResourceManager.Current["Back"];
            }
        }
    }
}
