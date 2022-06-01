using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
	/// <summary>
	/// EqualParameter is an <see cref="IValueConverter"/> which converts a given value to a boolean indicating if the value
	/// is equal to the provided <see cref="Binding.ConverterParameter"/>.
	/// </summary>
	public class EqualsParameter : IValueConverter, IMarkupExtension<EqualsParameter>
	{
		/// <summary>
		/// Returns <c>true</c> if <paramref name="value"/> equals to <paramref name="parameter"/> and <c>false</c> otherwise.
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? parameter == null : value.Equals(parameter);
		}

		/// <summary>
		/// Always throws a <see cref="NotImplementedException"/>.
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns an instance of <see cref="EqualsParameter"/> class.
		/// </summary>
		public EqualsParameter ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		/// <summary>
		/// Returns an instance of <see cref="EqualsParameter"/> class.
		/// </summary>
		object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		{
			return this.ProvideValue(serviceProvider);
		}
	}
}
