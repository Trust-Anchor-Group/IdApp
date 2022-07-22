using System;
using System.Globalization;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
	/// <summary>
	/// Converts a contract state to a color.
	/// </summary>
	public class ContractStateToColor : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is ContractState State)
				return ToColor(State);
			else
				return Color.Transparent;
		}

		/// <summary>
		/// Converts a contract state to a representative color.
		/// </summary>
		/// <param name="State">Contract State</param>
		/// <returns>Color</returns>
		public static Color ToColor(ContractState State)
		{
			return State switch
			{
				ContractState.Signed => Color.LightGreen,
				ContractState.Proposed or
				ContractState.Approved or
				ContractState.BeingSigned => Color.LightYellow,
				_ => Color.LightSalmon,
			};
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
