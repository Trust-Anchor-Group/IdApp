using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using System.ComponentModel;
using UIKit;
using Foundation;
using System;
using CoreGraphics;
using IdApp.Controls.Extended;
using IdApp.iOS.Renderers;

[assembly: ExportRenderer(typeof(ExtendedDatePicker), typeof(ExtendedDatePickeriOSRenderer))]
namespace IdApp.iOS.Renderers
{
    /// <summary>
    ///  Extended DatePicker Renderer for nullable values with text placeholder
    /// </summary>
    public class ExtendedDatePickeriOSRenderer : DatePickerRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
        {
            base.OnElementChanged(e);

			if (this.Element is ExtendedDatePicker DatePicker)
			{
				this.SetFont(DatePicker);
				this.SetTextAlignment(DatePicker);
				this.SetBorder(DatePicker);
				this.SetNullableText(DatePicker);
				this.SetPlaceholderTextColor(DatePicker);

				this.ResizeHeight();
			}
		}

        protected override void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(Sender, e);

			if (this.Element is ExtendedDatePicker DatePicker)
			{
				if (e.PropertyName == ExtendedDatePicker.FontProperty.PropertyName)
					this.SetFont(DatePicker);
				else if (e.PropertyName == ExtendedDatePicker.XAlignProperty.PropertyName)
					this.SetTextAlignment(DatePicker);
				else if (e.PropertyName == ExtendedDatePicker.HasBorderProperty.PropertyName)
					this.SetBorder(DatePicker);
				else if (e.PropertyName == ExtendedDatePicker.NullableDateProperty.PropertyName)
					this.SetNullableText(DatePicker);
				else if (e.PropertyName == ExtendedDatePicker.PlaceholderTextColorProperty.PropertyName)
					this.SetPlaceholderTextColor(DatePicker);

				this.ResizeHeight();
			}
        }

        private void SetTextAlignment(ExtendedDatePicker DatePicker)
        {
            switch (DatePicker.XAlign)
            {
                case TextAlignment.Center:
					this.Control.TextAlignment = UITextAlignment.Center;
                    break;
                case TextAlignment.End:
					this.Control.TextAlignment = UITextAlignment.Right;
                    break;
                case TextAlignment.Start:
					this.Control.TextAlignment = UITextAlignment.Left;
                    break;
            }
        }

        private void SetFont(ExtendedDatePicker DatePicker)
        {
            UIFont UiFont;
            if (DatePicker.Font != Font.Default && (UiFont = DatePicker.Font.ToUIFont()) != null)
                this.Control.Font = UiFont;
            else if (DatePicker.Font == Font.Default)
                this.Control.Font = UIFont.SystemFontOfSize(17f);
        }

        private void SetBorder(ExtendedDatePicker DatePicker)
        {
            this.Control.BorderStyle = DatePicker.HasBorder ? UITextBorderStyle.RoundedRect : UITextBorderStyle.None;
        }

        private void SetNullableText(ExtendedDatePicker DatePicker)
        {
            if (DatePicker.NullableDate == null)
                this.Control.Text = DatePicker.Placeholder;
        }

        private void ResizeHeight()
        {
            if (this.Element.HeightRequest >= 0) return;

            double Height = Math.Max(this.Bounds.Height,
                new UITextField { Font = this.Control.Font }.IntrinsicContentSize.Height) * 2;

			this.Control.Frame = new CGRect(0.0f, 0.0f, (nfloat)this.Element.Width, (nfloat)Height);
			this.Element.HeightRequest = Height;
        }

        private void SetPlaceholderTextColor(ExtendedDatePicker DatePicker)
        {
            if (!string.IsNullOrEmpty(DatePicker.Placeholder))
            {
				this.Control.AttributedPlaceholder = new NSAttributedString(DatePicker.Placeholder, this.Control.Font,
					DatePicker.PlaceholderTextColor.ToUIColor(), DatePicker.BackgroundColor.ToUIColor());
            }
        }
    }
}
