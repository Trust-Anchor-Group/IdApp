using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TextType = Xamarin.Forms.TextType;

namespace IdApp.Converters
{
    /// <summary>
    /// Converts a boolean value to a <see cref="TextType"/> value, which is <c>Html</c> if <c>true</c>, or <c>Text</c> if <c>false</c>.
    /// </summary>
    public class BooleanToTextTypeConverter : IValueConverter, IMarkupExtension
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TextType.Html : TextType.Text;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (TextType)value == TextType.Html;
        }

        /// <inheritdoc/>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}