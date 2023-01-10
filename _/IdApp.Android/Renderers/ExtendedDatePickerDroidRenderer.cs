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
        public ExtendedDatePickerDroidRenderer(Context Context)
            : base(Context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
        {
            base.OnElementChanged(e);

			if (this.Element is ExtendedDatePicker DatePicker)
			{
				this.SetFont(DatePicker);
				this.SetTextAlignment(DatePicker);
				// SetBorder(view);
				this.SetNullableText(DatePicker);
				this.SetPlaceholder(DatePicker);
				this.SetPlaceholderTextColor(DatePicker);
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
				// else if (e.PropertyName == ExtendedDatePicker.HasBorderProperty.PropertyName)
				//  SetBorder(view);
				else if (e.PropertyName == ExtendedDatePicker.NullableDateProperty.PropertyName)
					this.SetNullableText(DatePicker);
				else if (e.PropertyName == ExtendedDatePicker.PlaceholderProperty.PropertyName)
					this.SetPlaceholder(DatePicker);
				else if (e.PropertyName == ExtendedDatePicker.PlaceholderTextColorProperty.PropertyName)
					this.SetPlaceholderTextColor(DatePicker);
			}

        }

        private void SetTextAlignment(ExtendedDatePicker DatePicker)
        {
            switch (DatePicker.XAlign)
            {
                case Xamarin.Forms.TextAlignment.Center:
                    this.Control.Gravity = GravityFlags.CenterHorizontal;
                    break;
                case Xamarin.Forms.TextAlignment.End:
					this.Control.Gravity = GravityFlags.End;
                    break;
                case Xamarin.Forms.TextAlignment.Start:
					this.Control.Gravity = GravityFlags.Start;
                    break;
            }
        }

		private void SetFont(ExtendedDatePicker DatePicker)
        {
            if (DatePicker.Font != Font.Default)
            {
				this.Control.TextSize = DatePicker.Font.ToScaledPixel();
				this.Control.Typeface = DatePicker.Font.ToTypeface();
            }
        }

		private void SetNullableText(ExtendedDatePicker DatePicker)
        {
            if (!DatePicker.NullableDate.HasValue)
                this.Control.Text = string.Empty;
        }

		private void SetPlaceholder(ExtendedDatePicker DatePicker)
        {
            this.Control.Hint = DatePicker.Placeholder;
        }

		private void SetPlaceholderTextColor(ExtendedDatePicker DatePicker)
        {
            if (DatePicker.PlaceholderTextColor != Color.Default)
            {
                this.Control.SetHintTextColor(DatePicker.PlaceholderTextColor.ToAndroid());
            }
        }
    }
}
