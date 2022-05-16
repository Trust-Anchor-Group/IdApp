using System;
using System.Globalization;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
	/// <summary>
	/// Converts an identity state to a color.
	/// </summary>
	public class IdentityStateToColor : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is IdentityState State)
				return ToColor(State);
			else
				return Color.Transparent;
		}

		/// <summary>
		/// Converts a contract state to a representative color.
		/// </summary>
		/// <param name="State">Contract State</param>
		/// <returns>Color</returns>
		public static Color ToColor(IdentityState State)
		{
			switch (State)
			{
				case IdentityState.Approved:
					return Color.LightGreen;

				case IdentityState.Created:
					return Color.LightYellow;

				case IdentityState.Compromised:
				case IdentityState.Obsoleted:
				case IdentityState.Rejected:
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
