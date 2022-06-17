﻿using System.ComponentModel;
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

			float CornerRadius = this.Context.ToPixels(cornerRadiusInDip);

			if (this.Control is not null)
			{
				this.Control.Background = null;
				this.Control.SetBackgroundColor(AndroidColor.Transparent);

				// We should set padding on the Control, not on the ViewGroup. Otherwise, the ViewGroup's padding will clip the Control's content.
				this.Control.SetPadding((int)CornerRadius, (int)CornerRadius, (int)CornerRadius, (int)CornerRadius);
			}

			this.OverrideBackground();
		}

		protected override void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs EventArgs)
		{
			base.OnElementPropertyChanged(Sender, EventArgs);

			if (EventArgs.PropertyName == EntryProperties.BorderColorProperty.PropertyName || EventArgs.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
			{
				this.OverrideBackground();
			}
		}

		private void OverrideBackground()
		{
			RoundRectShape Shape = new(Enumerable.Repeat(this.Context.ToPixels(cornerRadiusInDip), 8).ToArray(), null, null);

			ShapeDrawable Border = new(Shape);
			Border.Paint.SetStyle(Paint.Style.Stroke);
			Border.Paint.StrokeWidth = this.Context.ToPixels(borderWidthInDip);
			Border.Paint.Color = this.Element != null ? EntryProperties.GetBorderColor(this.Element).ToAndroid() : AndroidColor.Transparent;

			ShapeDrawable Fill = new(Shape);
			Fill.Paint.SetStyle(Paint.Style.Fill);
			Fill.Paint.Color = this.Element != null ? this.Element.BackgroundColor.ToAndroid() : AndroidColor.Transparent;

			LayerDrawable Background = new(new Drawable[] { Fill, Border });

			this.Background = Background;
		}
	}
}
