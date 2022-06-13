using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using IdApp.iOS.Renderers;
using CoreGraphics;

[assembly: ExportRenderer(typeof(Entry), typeof(CustomEntryRenderer), new[] { typeof(VisualMarker.DefaultVisual) })]
namespace IdApp.iOS.Renderers
{
	public class CustomEntryRenderer : EntryRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);
			if (this.Control is not null)
			{
				this.Control.Layer.BorderWidth = 1;
				this.Control.Layer.CornerRadius = 5;
				this.Control.BorderStyle = UITextBorderStyle.Line;

				this.Control.LeftView = new UIView(new CGRect(0, 0, 5, 5));
				this.Control.LeftViewMode = UITextFieldViewMode.Always;
			}
		}
}
