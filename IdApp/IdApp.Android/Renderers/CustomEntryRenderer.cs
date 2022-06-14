using System.ComponentModel;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using IdApp.Android.Renderers;
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

			float CornerRadius = this.Context.ToPixels(cornerRadiusInDip);

			if (this.Control is not null)
			{
				this.Control.Background = null;
				this.Control.SetBackgroundColor(AndroidColor.Transparent);

				// We should set padding on the Control, not on the ViewGroup. Otherwise, the ViewGroup's padding will clip the Control's content.
				this.Control.SetPadding((int)CornerRadius, (int)CornerRadius, (int)CornerRadius, (int)CornerRadius);
			}

			// However, we should set the background itself on the ViewGroup, not on the Control, because the VisualElement.Background property,
			// which we are going to override, is rendered by the ViewGroup, not by the Control.
			ShapeDrawable Background = new(new RoundRectShape(Enumerable.Repeat(CornerRadius, 8).ToArray(), null, null));

			Background.Paint.SetStyle(Paint.Style.Stroke);
			Background.Paint.StrokeWidth = this.Context.ToPixels(borderWidthInDip);
			Background.Paint.Color = this.Element?.BackgroundColor.ToAndroid() ?? AndroidColor.Transparent;

			this.Background = Background;
		}

		protected override void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs EventArgs)
		{
			base.OnElementPropertyChanged(Sender, EventArgs);

			if (EventArgs.PropertyName == VisualElement.BackgroundColorProperty.PropertyName && this.Background is ShapeDrawable ShapeDrawable)
			{
				ShapeDrawable.Paint.Color = this.Element.BackgroundColor.ToAndroid();
			}
		}
	}
}
