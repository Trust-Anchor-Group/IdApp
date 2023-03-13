using FFImageLoading.Forms;
using System;
using Xamarin.Forms;

namespace IdApp.Helpers
{
	/// <summary>
	/// </summary>
	public class MyCachedImage : CachedImage
	{
		/// <inheritdoc/>
		protected override ImageSource CoerceImageSource(object NewValue)
		{
			UriImageSource UriImageSource = NewValue as UriImageSource;

			if ((UriImageSource?.Uri?.OriginalString is not null) &&
				UriImageSource.Uri.Scheme.Equals("aes256", StringComparison.OrdinalIgnoreCase))
			{
				return new AesImageSource(UriImageSource.Uri);
			}

			return base.CoerceImageSource(NewValue);
		}
	}
}
