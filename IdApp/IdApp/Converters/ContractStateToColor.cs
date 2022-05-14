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
			switch (State)
			{
				case ContractState.Signed:
					return Color.LightGreen;

				case ContractState.Proposed:
				case ContractState.Approved:
				case ContractState.BeingSigned:
					return Color.LightYellow;

				case ContractState.Deleted:
				case ContractState.Failed:
				case ContractState.Obsoleted:
				case ContractState.Rejected:
				default:
					return Color.LightSalmon;
			}
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
