using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;

namespace XamarinApp
{
	public static class QR
	{
		public static byte[] GenerateCodePng(string text, int width, int height)
		{
			BarcodeWriterPixelData writer = new BarcodeWriterPixelData()
			{
				Format = BarcodeFormat.QR_CODE,
				Options = new QrCodeEncodingOptions
				{
					Width = width,
					Height = height
				}
			};

			PixelData qrCode = writer.Write(text);
			width = qrCode.Width;
			height = qrCode.Height;

			int size = width * height << 2;

			using (SKData data = SKData.Create(size))
			{
				Marshal.Copy(qrCode.Pixels, 0, data.Data, size);

				using (SKImage result = SKImage.FromPixels(new SKImageInfo(width, height, SKColorType.Bgra8888), data, width << 2))
				{
					using (SKData encoded = result.Encode(SKEncodedImageFormat.Png, 100))
					{
						return encoded.ToArray();
					}
				}
			}
		}
	}
}
