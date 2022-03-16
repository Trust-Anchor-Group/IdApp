using IdApp.Controls.NoBounceView;
using IdApp.iOS.Renderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NoBounceScrollView), typeof(NoBounceScrollViewRenderer))]

namespace IdApp.iOS.Renderers
{
    public class NoBounceScrollViewRenderer : ScrollViewRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            UpdateScrollView();
        }
 
        private void UpdateScrollView()
        {
            ContentInset = new UIEdgeInsets(0, 0, 0, 0);
            ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
            Bounces = false;
            ScrollIndicatorInsets = new UIEdgeInsets(0, 0, 0, 0);
        }
    }
}