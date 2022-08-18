using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using IdApp.iOS.Renderers;
using CoreGraphics;
using IdApp.Helpers;
using System.ComponentModel;

[assembly: ExportRenderer(typeof(Picker), typeof(CustomPickerRenderer), new[] { typeof(VisualMarker.DefaultVisual) })]
namespace IdApp.iOS.Renderers
{
	public class CustomPickerRenderer : PickerRenderer
	{
		protected override void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs EventArgs)
		{
			base.OnElementPropertyChanged(Sender, EventArgs);

			if (EventArgs.PropertyName == EntryProperties.BorderColorProperty.PropertyName)
			{
				this.OverrideBackground();
			}
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
		{
			base.OnElementChanged(e);
			this.OverrideBackground();
		}

		private void OverrideBackground()
		{
			if (this.Control is not null)
			{
				this.Control.Layer.BorderWidth = (System.nfloat)(this.Element is not null ? EntryProperties.GetBorderWidth(this.Element) : 0);
				this.Control.Layer.CornerRadius = (System.nfloat)(this.Element is not null ? EntryProperties.GetCornerRadius(this.Element) : 0);
				this.Control.BorderStyle = UITextBorderStyle.Line;
				this.Control.Layer.BorderColor = this.Element is not null ? EntryProperties.GetBorderColor(this.Element).ToCGColor() : UIColor.Clear.CGColor;

				this.Control.LeftView = new UIView(new CGRect(0, 0, 5, 5));
				this.Control.LeftViewMode = UITextFieldViewMode.Always;
			}
		}
	}
}
