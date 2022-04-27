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

            var view = Element as ExtendedDatePicker;

            if (view != null)
            {
                SetFont(view);
                SetTextAlignment(view);
                SetBorder(view);
                SetNullableText(view);
                SetPlaceholderTextColor(view);

                ResizeHeight();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            var view = (ExtendedDatePicker)Element;

            if (e.PropertyName == ExtendedDatePicker.FontProperty.PropertyName)
                SetFont(view);
            else if (e.PropertyName == ExtendedDatePicker.XAlignProperty.PropertyName)
                SetTextAlignment(view);
            else if (e.PropertyName == ExtendedDatePicker.HasBorderProperty.PropertyName)
                SetBorder(view);
            else if (e.PropertyName == ExtendedDatePicker.NullableDateProperty.PropertyName)
                SetNullableText(view);
            else if (e.PropertyName == ExtendedDatePicker.PlaceholderTextColorProperty.PropertyName)
                SetPlaceholderTextColor(view);

            ResizeHeight();
        }

        /// <summary>
        /// Sets the text alignment.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetTextAlignment(ExtendedDatePicker view)
        {
            switch (view.XAlign)
            {
                case TextAlignment.Center:
                    Control.TextAlignment = UITextAlignment.Center;
                    break;
                case TextAlignment.End:
                    Control.TextAlignment = UITextAlignment.Right;
                    break;
                case TextAlignment.Start:
                    Control.TextAlignment = UITextAlignment.Left;
                    break;
            }
        }

        /// <summary>
        /// Sets the font.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetFont(ExtendedDatePicker view)
        {
            UIFont uiFont;
            if (view.Font != Font.Default && (uiFont = view.Font.ToUIFont()) != null)
                Control.Font = uiFont;
            else if (view.Font == Font.Default)
                Control.Font = UIFont.SystemFontOfSize(17f);
        }

        /// <summary>
        /// Sets the border.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetBorder(ExtendedDatePicker view)
        {
            Control.BorderStyle = view.HasBorder ? UITextBorderStyle.RoundedRect : UITextBorderStyle.None;
        }

        /// <summary>
        /// Set text based on nullable value
        /// </summary>
        /// <param name="view"></param>
        private void SetNullableText(ExtendedDatePicker view)
        {
            if (view.NullableDate == null)
                Control.Text = view.Placeholder;
        }

        /// <summary>
        /// Resizes the height.
        /// </summary>
        private void ResizeHeight()
        {
            if (Element.HeightRequest >= 0) return;

            var height = Math.Max(Bounds.Height,
                new UITextField { Font = Control.Font }.IntrinsicContentSize.Height) * 2;

            Control.Frame = new CGRect(0.0f, 0.0f, (nfloat)Element.Width, (nfloat)height);

            Element.HeightRequest = height;
        }

        /// <summary>
        /// Sets the color of the placeholder text.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetPlaceholderTextColor(ExtendedDatePicker view)
        {
            if (!string.IsNullOrEmpty(view.Placeholder))
            {
                var backgroundUIColor = view.BackgroundColor.ToUIColor();
                var foregroundUIColor = view.PlaceholderTextColor.ToUIColor();
                var targetFont = Control.Font;
                Control.AttributedPlaceholder = new NSAttributedString(view.Placeholder, targetFont, foregroundUIColor, backgroundUIColor);
            }
        }
    }
}
