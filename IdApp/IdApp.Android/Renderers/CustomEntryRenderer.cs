using Android.Content;
using IdApp.Android.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Entry), typeof(CustomEntryRenderer), new[] { typeof(VisualMarker.DefaultVisual) })]
namespace IdApp.Android.Renderers
{
	public class CustomEntryRenderer : EntryRenderer
	{
		public CustomEntryRenderer(Context context) : base(context) { }

		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
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
