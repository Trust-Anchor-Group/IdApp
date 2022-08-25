using IdApp.Extensions;
using System;
using System.Globalization;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
    /// <summary>
    /// Converts a boolean value to either Yes or No
    /// </summary>
    public class BooleanToYesNo : IValueConverter, IMarkupExtension
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value).ToYesNo();
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == LocalizationResourceManager.Current["Yes"];
        }

        /// <inheritdoc/>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
