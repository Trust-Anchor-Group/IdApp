using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using IdApp.Android.Renderers;
using IdApp.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Android;

[assembly: ExportImageSourceHandler(typeof(AesImageSource), typeof(AesImageLoaderSourceHandler))]
namespace IdApp.Android.Renderers
{
	public class AesImageLoaderSourceHandler : IAnimationSourceHandler, IImageSourceHandler
	{
		public async Task<Bitmap> LoadImageAsync(ImageSource ImageSource, Context Context, CancellationToken CancelationToken = default)
		{
			AesImageSource ImageLoader = ImageSource as AesImageSource;
			Bitmap Bitmap = null;

			if (ImageLoader?.Uri is not null)
			{
				using Stream ImageStream = await ImageLoader.GetStreamAsync(CancelationToken).ConfigureAwait(false);

				Bitmap = await BitmapFactory.DecodeStreamAsync(ImageStream).ConfigureAwait(false);
			}

			if (Bitmap is null)
			{
				Log.Warning(nameof(AesImageLoaderSourceHandler), "Could not retrieve image or image data was invalid: {0}", ImageLoader);
			}

			return Bitmap;
		}

		public Task<IFormsAnimationDrawable> LoadImageAnimationAsync(ImageSource Imagesource, Context Context, CancellationToken CancelationToken = default, float Scale = 1)
		{
			return FormsAnimationDrawable.LoadImageAnimationAsync(Imagesource, Context, CancelationToken);
		}
	}
}
