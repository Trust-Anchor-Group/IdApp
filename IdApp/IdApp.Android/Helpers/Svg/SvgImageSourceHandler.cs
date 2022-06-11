using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using IdApp.Helpers.Svg;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace IdApp.Android.Helpers.Svg
{
	/// <summary>
	/// Svg image source handler.
	/// </summary>
	public class SvgImageSourceHandler : IImageSourceHandler
	{
		/// <summary>
		/// Loads the image async.
		/// </summary>
		/// <returns>The image async.</returns>
		/// <param name="Imagesource">Imagesource.</param>
		/// <param name="Context">Context.</param>
		/// <param name="CancelationToken">Cancelation token.</param>
		public async Task<Bitmap> LoadImageAsync(ImageSource ImageSource, Context Context, CancellationToken CancelationToken = default)
		{
			if (ImageSource is SvgImageSource SvgImageSource)
			{
				using (System.IO.Stream ImageStream = await SvgImageSource.GetImageStreamAsync(CancelationToken).ConfigureAwait(false))
				{
					if (ImageStream is not null)
					{
						return await BitmapFactory.DecodeStreamAsync(ImageStream);
					}
				}
			}

			return null;
		}
	}
}
