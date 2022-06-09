using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using IdApp.iOS.Renderers;

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
				this.Control.BackgroundColor = UIColor.FromWhiteAlpha(1, 1);
				this.Control.Layer.BorderWidth = 0;
				this.Control.BorderStyle = UITextBorderStyle.None;
			}
		}
	}
}
