using System;
using Xamarin.Forms.Platform.iOS;

namespace IdApp.iOS.Renderers
{
    internal class IdAppShellSectionRenderer : ShellSectionRenderer
    {
        public IdAppShellSectionRenderer(IShellContext Context) : base(Context)
        {
        }

        public IdAppShellSectionRenderer(IShellContext Context, Type NavigationBarType, Type ToolbarType) : base(Context, NavigationBarType, ToolbarType)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.InteractivePopGestureRecognizer.Enabled = false;
        }
    }
}
