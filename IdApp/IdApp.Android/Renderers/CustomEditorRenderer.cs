using Android.Content;
using IdApp.Android.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Editor), typeof(CustomEditorRenderer), new[] { typeof(VisualMarker.DefaultVisual) })]
namespace IdApp.Android.Renderers
{
	public class CustomEditorRenderer : EditorRenderer
	{
		public CustomEditorRenderer(Context context) : base(context) { }

		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
			base.OnElementChanged(e);

			if (this.Control is not null)
			{
				this.Control.Background = null;
				this.Control.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
			}
		}
	}
}
