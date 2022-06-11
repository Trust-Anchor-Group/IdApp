using Android.Content;
using Android.Util;
using IdApp.Helpers.Svg;

namespace IdApp.Android.Helpers.Svg
{
	/// <summary>
	/// Svg image.
	/// </summary>
	public static class SvgImage
	{
		/// <summary>
		/// Init this instance.
		/// </summary>
		public static void Init(Context Context)
		{
			Xamarin.Forms.Internals.Registrar.Registered.Register(typeof(SvgImageSource), typeof(SvgImageSourceHandler));

			using (DisplayMetrics Display = Context.Resources.DisplayMetrics)
			{
				SvgImageSource.ScreenScale = Display.Density;
			}
		}
	}
}
