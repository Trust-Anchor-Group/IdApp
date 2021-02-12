using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
    /// <summary>
    /// Converts a boolean value to a bold font (if true), or regular font (if false).
    /// </summary>
    public class BooleanToBoldFontConverter : IValueConverter, IMarkupExtension
    {
        /// <summary>
        /// Set to true if the conversion should be inverted or not.
        /// </summary>
        public bool Invert { get; set; }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Invert)
            {
                return (bool)value ? FontAttributes.None : FontAttributes.Bold;
            }
            return (bool)value ? FontAttributes.Bold : FontAttributes.None;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Invert)
            {
                return (FontAttributes)value != FontAttributes.Bold;
            }
            return (FontAttributes)value == FontAttributes.Bold;
        }

        /// <inheritdoc/>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}