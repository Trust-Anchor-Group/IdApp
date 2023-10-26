using System;
using System.Globalization;
using System.Text;
using Waher.Content;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
	/// <summary>
	/// Converts a <see cref="Duration"/> value to a <see cref="String"/> value.
	/// </summary>
	public class DurationToString : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not Duration D)
			{
				if (value is string s)
				{
					if (!Duration.TryParse(s, out D))
						return string.Empty;
				}
				else
					return string.Empty;
			}

			return ToString(D);
		}

		/// <summary>
		/// Converts a Duration to a human-readable text.
		/// </summary>
		/// <param name="Duration">Duration to convert.</param>
		/// <returns>Human-readable text.</returns>
		public static string ToString(Duration Duration)
		{
			StringBuilder sb = new StringBuilder();
			bool First = true;

			if (Duration.Negation)
			{
				sb.Append(LocalizationResourceManager.Current["Negative"]);
				sb.Append(' ');
			}

			Append(sb, Duration.Years, ref First, LocalizationResourceManager.Current["Year"], LocalizationResourceManager.Current["Years"]);
			Append(sb, Duration.Months, ref First, LocalizationResourceManager.Current["Month"], LocalizationResourceManager.Current["Months"]);
			Append(sb, Duration.Days, ref First, LocalizationResourceManager.Current["Day"], LocalizationResourceManager.Current["Days"]);
			Append(sb, Duration.Hours, ref First, LocalizationResourceManager.Current["Hour"], LocalizationResourceManager.Current["Hours"]);
			Append(sb, Duration.Minutes, ref First, LocalizationResourceManager.Current["Minute"], LocalizationResourceManager.Current["Minutes"]);
			Append(sb, Duration.Seconds, ref First, LocalizationResourceManager.Current["Second"], LocalizationResourceManager.Current["Seconds"]);

			if (First)
				sb.Append('-');

			return sb.ToString();
		}

		private static void Append(StringBuilder sb, int Nr, ref bool First, string SingularUnit, string PluralUnit)
		{
			if (Nr != 0)
			{
				if (First)
					First = false;
				else
					sb.Append(", ");

				sb.Append(Nr.ToString());
				sb.Append(' ');

				if (Nr == 1)
					sb.Append(SingularUnit);
				else
					sb.Append(PluralUnit);
			}
		}

		private static void Append(StringBuilder sb, double Nr, ref bool First, string SingularUnit, string PluralUnit)
		{
			if (Nr != 0)
			{
				if (First)
					First = false;
				else
					sb.Append(", ");

				sb.Append(Nr.ToString());
				sb.Append(' ');

				if (Nr == 1)
					sb.Append(SingularUnit);
				else
					sb.Append(PluralUnit);
			}
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string s && Duration.TryParse(s, out Duration D))
				return D;
			else
				return Duration.Zero;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
