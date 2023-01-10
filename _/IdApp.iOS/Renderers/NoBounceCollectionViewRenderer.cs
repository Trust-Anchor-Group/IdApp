using IdApp.Controls.NoBounceView;
using IdApp.iOS.Renderers;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NoBounceCollectionView), typeof(NoBounceCollectionViewRenderer))]

namespace IdApp.iOS.Renderers
{
    public class NoBounceCollectionViewRenderer : CollectionViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<GroupableItemsView> e)
        {
            base.OnElementChanged(e);
			this.UpdateScrollView();
        }

        private void UpdateScrollView()
        {
            if (this.Controller is not null)
            {
                UICollectionView CollectionView = this.Controller.CollectionView;

                if (CollectionView is not null)
                {
                    CollectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
                    CollectionView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
                    CollectionView.Bounces = false;
                    CollectionView.ScrollIndicatorInsets = new UIEdgeInsets(0, 0, 0, 0);
                }
            }
        }
    }
}
