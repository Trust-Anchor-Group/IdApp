using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using IdApp.iOS.Renderers;

[assembly: ExportRenderer(typeof(Editor), typeof(CustomEditorRenderer), new[] { typeof(VisualMarker.DefaultVisual) })]
namespace IdApp.iOS.Renderers
{
	public class CustomEditorRenderer : EditorRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
			base.OnElementChanged(e);
			if (this.Control is not null)
			{
				this.Control.BackgroundColor = UIColor.FromWhiteAlpha(1, 1);
				this.Control.Layer.BorderWidth = 0;
			}
		}
	}
}
