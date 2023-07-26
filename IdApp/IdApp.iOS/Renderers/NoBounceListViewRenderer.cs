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

            if (this.Element is not null)
            {
                this.Control.AlwaysBounceVertical = this.Element.IsPullToRefreshEnabled;
				this.Control.Bounces = this.Element.IsPullToRefreshEnabled;
            }
        }

        protected override void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(Sender, e);

            if (e.PropertyName == nameof(this.Element.IsPullToRefreshEnabled))
            {
                this.Control.AlwaysBounceVertical = this.Element.IsPullToRefreshEnabled;
				this.Control.Bounces = this.Element.IsPullToRefreshEnabled;
            }
        }
    }
}
