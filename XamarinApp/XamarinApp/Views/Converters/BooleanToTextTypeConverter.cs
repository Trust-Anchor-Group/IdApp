using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TextType = Xamarin.Forms.TextType;

namespace XamarinApp.Views.Converters
{
    public class BooleanToTextTypeConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TextType.Html : TextType.Text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (TextType)value == TextType.Html;
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}