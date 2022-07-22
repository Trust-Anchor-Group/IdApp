using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IdApp.Helpers.Svg
{
	/// <summary>
	/// Svg utility.
	/// </summary>
	public static class SvgUtility
	{
		/// <summary>
		/// Creates the image.
		/// </summary>
		/// <returns>The image.</returns>
		/// <param name="Stream">Stream.</param>
		/// <param name="Width">Width.</param>
		/// <param name="Height">Height.</param>
		/// <param name="Color">Color.</param>
		public static Task<Stream> CreateImage(Stream Stream, double Width, double Height, Color Color)
		{
			float ScreenScale = SvgImageSource.ScreenScale;

			using SKSvg Svg = new();
			Svg.Load(Stream);

			SKSize SvgSize = Svg.Picture.CullRect.Size;
			SKSize Size = CalcSize(SvgSize, Width, Height);
			Tuple<float, float> Scale = CalcScale(SvgSize, Size, ScreenScale);
			SKMatrix Matrix = SKMatrix.CreateScale(Scale.Item1, Scale.Item2);

			using SKBitmap Bitmap = new((int)(Size.Width * ScreenScale), (int)(Size.Height * ScreenScale));
			using SKCanvas Canvas = new(Bitmap);
			using SKPaint Paint = new();

			if (!Color.IsDefault)
				Paint.ColorFilter = SKColorFilter.CreateBlendMode(ToSKColor(Color), SKBlendMode.SrcIn);

			Canvas.Clear(SKColors.Transparent); // very very important!
			Canvas.DrawPicture(Svg.Picture, ref Matrix, Paint);

			using SKImage Image = SKImage.FromBitmap(Bitmap);
			using SKData Encoded = Image.Encode();
			using MemoryStream ImageStream = new();

			Encoded.SaveTo(ImageStream);
			ImageStream.Position = 0;

			return Task.FromResult(ImageStream as Stream);
		}

		/// <summary>
		/// Tos the SKC olor.
		/// </summary>
		/// <returns>The SKC olor.</returns>
		/// <param name="color">Color.</param>
		public static SKColor ToSKColor(Color color)
		{
			return new SKColor((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255), (byte)(color.A * 255));
		}

		/// <summary>
		/// Calculates the size.
		/// </summary>
		/// <returns>The size.</returns>
		/// <param name="Size">Size.</param>
		/// <param name="Width">Width.</param>
		/// <param name="Height">Height.</param>
		public static SKSize CalcSize(SkiaSharp.SKSize Size, double Width, double Height)
		{
			double w;
			double h;

			if (Width <= 0 && Height <= 0)
			{
				return Size;
			}
			else if (Width <= 0)
			{
				h = Height;
				w = Height * (Size.Width / Size.Height);
			}
			else if (Height <= 0)
			{
				w = Width;
				h = Width * (Size.Height / Size.Width);
			}
			else
			{
				w = Width;
				h = Height;
			}

			return new SKSize((float)w, (float)h);
		}

		/// <summary>
		/// Calculates the scale.
		/// </summary>
		/// <returns>The scale.</returns>
		/// <param name="OriginalSize">Original size.</param>
		/// <param name="ScaledSize">Scaled size.</param>
		/// <param name="ScreenScale">Screen scale.</param>
		public static Tuple<float, float> CalcScale(SkiaSharp.SKSize OriginalSize, SkiaSharp.SKSize ScaledSize, float ScreenScale)
		{
			float sx = ScaledSize.Width * ScreenScale / OriginalSize.Width;
			float sy = ScaledSize.Height * ScreenScale / OriginalSize.Height;

			return new Tuple<float, float>(sx, sy);
		}
	}
}
