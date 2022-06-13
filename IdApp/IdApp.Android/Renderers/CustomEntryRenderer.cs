using System.ComponentModel;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using IdApp.Android.Renderers;
using IdApp.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using AndroidColor = Android.Graphics.Color;

[assembly: ExportRenderer(typeof(Entry), typeof(CustomEntryRenderer), new[] { typeof(VisualMarker.DefaultVisual) })]
namespace IdApp.Android.Renderers
{
	public class CustomEntryRenderer : EntryRenderer
	{
		private const double cornerRadiusInDip = 5;
		private const double borderWidthInDip = 1;

		public CustomEntryRenderer(Context context) : base(context) { }

		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);

			if (this.Control is not null)
			{
				float CornerRadius = this.Context.ToPixels(cornerRadiusInDip);
				ShapeDrawable Background = new(new RoundRectShape(Enumerable.Repeat(CornerRadius, 8).ToArray(), null, null));

				Background.Paint.SetStyle(Paint.Style.Stroke);
				Background.Paint.StrokeWidth = this.Context.ToPixels(borderWidthInDip);
				Background.Paint.Color = this.Element is Entry Entry ? EntryProperties.GetBorderColor(Entry).ToAndroid() : AndroidColor.Transparent;

				this.Control.Background = Background;
			}
		}

		protected override void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs EventArgs)
		{
			base.OnElementPropertyChanged(Sender, EventArgs);

			if (EventArgs.PropertyName == EntryProperties.BorderColorProperty.PropertyName && this.Control?.Background is ShapeDrawable ShapeDrawable)
			{
				ShapeDrawable.Paint.Color = EntryProperties.GetBorderColor(this.Element).ToAndroid();
			}
		}
	}
}
