using System;
using Xamarin.Forms;

namespace IdApp.Controls.Extended
{
    /// <summary>
    ///  Extended DatePicker for nullable values with text placeholder
    /// </summary>
    public class ExtendedDatePicker : DatePicker
    {
        /// <summary>
        /// The font property
        /// </summary>
        public static readonly BindableProperty FontProperty =
            BindableProperty.Create("Font", typeof(Font), typeof(ExtendedDatePicker), new Font());

        /// <summary>
        /// The NullableDate property
        /// </summary>
        public static readonly BindableProperty NullableDateProperty =
            BindableProperty.Create("NullableDate", typeof(DateTime?), typeof(ExtendedDatePicker), null, BindingMode.TwoWay,
            propertyChanged: DatePropertyChanged);

        /// <summary>
        /// The XAlign property
        /// </summary>
        public static readonly BindableProperty XAlignProperty =
            BindableProperty.Create("XAlign", typeof(TextAlignment), typeof(ExtendedDatePicker),
            TextAlignment.Start);

        /// <summary>
        /// The HasBorder property
        /// </summary>
        public static readonly BindableProperty HasBorderProperty =
            BindableProperty.Create("HasBorder", typeof(bool), typeof(ExtendedDatePicker), true);

        /// <summary>
        /// The Placeholder property
        /// </summary>
        public static readonly BindableProperty PlaceholderProperty =
            BindableProperty.Create("Placeholder", typeof(string), typeof(ExtendedDatePicker), string.Empty, BindingMode.OneWay);

        /// <summary>
        /// The PlaceholderTextColor property
        /// </summary>
        public static readonly BindableProperty PlaceholderTextColorProperty =
            BindableProperty.Create("PlaceholderTextColor", typeof(Color), typeof(ExtendedDatePicker), Color.Default);


        /// <summary>
        /// Gets or sets the Font
        /// </summary>
        public Font Font
        {
            get { return (Font)GetValue(FontProperty); }
            set { SetValue(FontProperty, value); }
        }

        /// <summary>
        /// Get or sets the NullableDate
        /// </summary>
        public DateTime? NullableDate
        {
            get { return (DateTime?)GetValue(NullableDateProperty); }
            set
            {
                if (value != NullableDate)
                {
                    SetValue(NullableDateProperty, value);
                    UpdateDate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the X alignment of the text
        /// </summary>
        public TextAlignment XAlign
        {
            get { return (TextAlignment)GetValue(XAlignProperty); }
            set { SetValue(XAlignProperty, value); }
        }


        /// <summary>
        /// Gets or sets if the border should be shown or not
        /// </summary>
        public bool HasBorder
        {
            get { return (bool)GetValue(HasBorderProperty); }
            set { SetValue(HasBorderProperty, value); }
        }

        /// <summary>
        /// Get or sets the PlaceHolder
        /// </summary>
        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        /// <summary>
        /// Sets color for placeholder text
        /// </summary>
        public Color PlaceholderTextColor
        {
            get { return (Color)GetValue(PlaceholderTextColorProperty); }
            set { SetValue(PlaceholderTextColorProperty, value); }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ExtendedDatePicker"/> class.
        /// </summary>
        public ExtendedDatePicker() : base()
        {
            SetDefaultDate();
        }

        /// <summary>
        /// Event sent when the date is changed.
        /// </summary>
        public event EventHandler<NullableDateChangedEventArgs> NullableDateSelected;

        static void DatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ExtendedDatePicker Picker = (ExtendedDatePicker)bindable;
            EventHandler<NullableDateChangedEventArgs> selected = Picker.NullableDateSelected;

            selected?.Invoke(Picker, new NullableDateChangedEventArgs((DateTime?)oldValue, (DateTime?)newValue));
        }

        private bool IsDefaultDateSet = false;

        /// <inheritdoc/>
        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == IsFocusedProperty.PropertyName)
            {
                // we don't know if the date picker was closed by the Ok or Cancel button,
                // so we presuppose it was an Ok.
                if (!IsFocused)
                {
                    OnPropertyChanged(DateProperty.PropertyName);
                }
            }

            if (propertyName == DateProperty.PropertyName && !IsDefaultDateSet)
            {
                NullableDate = Date;                
            }

            if (propertyName == NullableDateProperty.PropertyName)
            {
                if (NullableDate.HasValue)
                {
                    Date = NullableDate.Value;
                }
            }
        }

        private void UpdateDate()
        {
            if (NullableDate.HasValue)
            {
                Date = NullableDate.Value;
            }
            else
            {
                IsDefaultDateSet = true;
                SetDefaultDate();
                IsDefaultDateSet = false;
            }
        }

        private void SetDefaultDate()
        {
            DateTime now = DateTime.Now;
            Date = new DateTime(now.Year, now.Month, now.Day);
        }
    }
}
