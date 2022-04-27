using Android.Content;
using Android.Views;
using IdApp.Android.Renderers;
using IdApp.Controls.Extended;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ExtendedDatePicker), typeof(ExtendedDatePickerDroidRenderer))]
namespace IdApp.Android.Renderers
{
    /// <summary>
    ///  Extended DatePicker Renderer for nullable values with text placeholder
    /// </summary>
    public class ExtendedDatePickerDroidRenderer : DatePickerRenderer
    {
        public ExtendedDatePickerDroidRenderer(Context context)
            : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
        {
            base.OnElementChanged(e);

            ExtendedDatePicker view = Element as ExtendedDatePicker;

            if (view != null)
            {
                SetFont(view);
                SetTextAlignment(view);
                // SetBorder(view);
                SetNullableText(view);
                SetPlaceholder(view);
                SetPlaceholderTextColor(view);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            ExtendedDatePicker view = (ExtendedDatePicker)Element;

            if (e.PropertyName == ExtendedDatePicker.FontProperty.PropertyName)
                SetFont(view);
            else if (e.PropertyName == ExtendedDatePicker.XAlignProperty.PropertyName)
                SetTextAlignment(view);
            // else if (e.PropertyName == ExtendedDatePicker.HasBorderProperty.PropertyName)
            //  SetBorder(view);
            else if (e.PropertyName == ExtendedDatePicker.NullableDateProperty.PropertyName)
                SetNullableText(view);
            else if (e.PropertyName == ExtendedDatePicker.PlaceholderProperty.PropertyName)
                SetPlaceholder(view);
            else if (e.PropertyName == ExtendedDatePicker.PlaceholderTextColorProperty.PropertyName)
                SetPlaceholderTextColor(view);

        }

        /// <summary>
        /// Sets the text alignment.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetTextAlignment(ExtendedDatePicker view)
        {
            switch (view.XAlign)
            {
                case Xamarin.Forms.TextAlignment.Center:
                    Control.Gravity = GravityFlags.CenterHorizontal;
                    break;
                case Xamarin.Forms.TextAlignment.End:
                    Control.Gravity = GravityFlags.End;
                    break;
                case Xamarin.Forms.TextAlignment.Start:
                    Control.Gravity = GravityFlags.Start;
                    break;
            }
        }

        /// <summary>
        /// Sets the font.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetFont(ExtendedDatePicker view)
        {
            if (view.Font != Font.Default)
            {
                Control.TextSize = view.Font.ToScaledPixel();
                Control.Typeface = view.Font.ToTypeface();
            }
        }

        /// <summary>
        /// Set text based on nullable value
        /// </summary>
        /// <param name="view"></param>
        private void SetNullableText(ExtendedDatePicker view)
        {
            if (view.NullableDate == null)
                Control.Text = string.Empty;
        }

        /// <summary>
        /// Set the placeholder
        /// </summary>
        /// <param name="view"></param>
        private void SetPlaceholder(ExtendedDatePicker view)
        {
            Control.Hint = view.Placeholder;
        }

        /// <summary>
        /// Sets the color of the placeholder text.
        /// </summary>
        /// <param name="view">The view.</param>
        private void SetPlaceholderTextColor(ExtendedDatePicker view)
        {
            if (view.PlaceholderTextColor != Color.Default)
            {
                Control.SetHintTextColor(view.PlaceholderTextColor.ToAndroid());
            }
        }
    }
}