using IdApp.Helpers.Svg;
using UIKit;

namespace IdApp.iOS.Helpers.Svg
{
	internal class SvgImage
	{
		/// <summary>
		/// Init this instance.
		/// </summary>
		public static void Init()
		{
			Xamarin.Forms.Internals.Registrar.Registered.Register(typeof(SvgImageSource), typeof(SvgImageSourceHandler));

			SvgImageSource.ScreenScale = (float)UIScreen.MainScreen.Scale;
		}
	}
}
