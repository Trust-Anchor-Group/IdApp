using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
    /// <summary>
    /// Is true if an integer property is equal to one.
    /// </summary>
    public class EqualsOne : IValueConverter, IMarkupExtension
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i == 1;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? 1 : -1;
            else
                return -1;
        }

        /// <inheritdoc/>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}