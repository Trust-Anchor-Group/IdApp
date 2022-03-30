using System.ComponentModel;
using IdApp.Controls.NoBounceView;
using IdApp.iOS.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NoBounceListView), typeof(NoBounceListViewRenderer))]

namespace IdApp.iOS.Renderers
{
    public class NoBounceListViewRenderer : ListViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<ListView> e)
        {
            base.OnElementChanged(e);

            if (Element is not null)
            {
                Control.AlwaysBounceVertical = Element.IsPullToRefreshEnabled;
                Control.Bounces = Element.IsPullToRefreshEnabled;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(Element.IsPullToRefreshEnabled))
            {
                Control.AlwaysBounceVertical = Element.IsPullToRefreshEnabled;
                Control.Bounces = Element.IsPullToRefreshEnabled;
            }
        }
    }
}