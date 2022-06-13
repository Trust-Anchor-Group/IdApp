using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using IdApp.Helpers.Svg;

namespace IdApp.iOS.Helpers.Svg
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
		/// <param name="CancelationToken">Cancelation token.</param>
		public async Task<UIImage> LoadImageAsync(ImageSource Imagesource, CancellationToken CancelationToken = default, float Scale = 1)
		{
			if (Imagesource is SvgImageSource SvgImageSource)
			{
				using (System.IO.Stream ImageStream = await SvgImageSource.GetImageStreamAsync(CancelationToken).ConfigureAwait(false))
				{
					if (ImageStream is not null)
					{
						return UIImage.LoadFromData(NSData.FromStream(ImageStream), SvgImageSource.ScreenScale);
					}
				}
			}

			return null;
		}
	}
}
