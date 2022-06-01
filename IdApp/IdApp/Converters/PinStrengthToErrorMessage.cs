using System;
using System.Globalization;
using IdApp.Resx;
using IdApp.Services.Tag;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Converters
{
	/// <summary>
	/// PinStrengthToErrorMessage is an <see cref="IValueConverter"/> which converts <see cref="PinStrength"/> to an error message to display.
	/// </summary>
	public class PinStrengthToErrorMessage : IValueConverter, IMarkupExtension<PinStrengthToErrorMessage>
	{
		/// <summary>
		/// Returns a localized error message for a given <see cref="PinStrength"/>.
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is PinStrength PinStrength)
			{
				return PinStrength switch
				{
					PinStrength.NotEnoughDigitsLettersSigns => string.Format(AppResources.PinWithNotEnoughDigitsLettersSigns, Constants.Authentication.MinPinSymbolsFromDifferentClasses),

					PinStrength.NotEnoughDigitsOrSigns => string.Format(AppResources.PinWithNotEnoughDigitsOrSigns, Constants.Authentication.MinPinSymbolsFromDifferentClasses),
					PinStrength.NotEnoughLettersOrDigits => string.Format(AppResources.PinWithNotEnoughLettersOrDigits, Constants.Authentication.MinPinSymbolsFromDifferentClasses),
					PinStrength.NotEnoughLettersOrSigns => string.Format(AppResources.PinWithNotEnoughLettersOrSigns, Constants.Authentication.MinPinSymbolsFromDifferentClasses),

					PinStrength.ContainsAddress => AppResources.PinContainsAddress,
					PinStrength.ContainsName => AppResources.PinContainsName,
					PinStrength.ContainsPersonalNumber => AppResources.PinContainsPersonalNumber,
					PinStrength.ContainsPhoneNumber => AppResources.PinContainsPhoneNumber,

					PinStrength.TooManyIdenticalSymbols => string.Format(AppResources.PinWithTooManyIdenticalSymbols, Constants.Authentication.MaxPinIdenticalSymbols),
					PinStrength.TooManySequencedSymbols => string.Format(AppResources.PinWithTooManySequencedSymbols, Constants.Authentication.MaxPinSequencedSymbols),

					PinStrength.TooShort => string.Format(AppResources.PinTooShort, Constants.Authentication.MinPinLength),

					_ => "",
				};
			}

			throw new ArgumentException($"{nameof(Services.Tag.PinStrength)} expected but received {value?.GetType().Name ?? "null"}.");
		}

		/// <summary>
		/// Always throws a <see cref="NotImplementedException"/>.
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns an instance of <see cref="PinStrengthToErrorMessage"/> class.
		/// </summary>
		public PinStrengthToErrorMessage ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		/// <summary>
		/// Returns an instance of <see cref="PinStrengthToErrorMessage"/> class.
		/// </summary>
		object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		{
			return this.ProvideValue(serviceProvider);
		}
	}
}
