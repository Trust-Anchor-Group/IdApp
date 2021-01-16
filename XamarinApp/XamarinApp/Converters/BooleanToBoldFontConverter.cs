using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinApp.Converters
{
    public class BooleanToBoldFontConverter : IValueConverter, IMarkupExtension
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Invert)
            {
                return (bool)value ? FontAttributes.None : FontAttributes.Bold;
            }
            return (bool)value ? FontAttributes.Bold : FontAttributes.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Invert)
            {
                return (FontAttributes)value != FontAttributes.Bold;
            }
            return (FontAttributes)value == FontAttributes.Bold;
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}