using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;

namespace XamarinApp
{
	public static class QR
	{
		public static byte[] GenerateCodePng(string Text, int Width, int Height)
		{
			BarcodeWriterPixelData Writer = new BarcodeWriterPixelData()
			{
				Format = BarcodeFormat.QR_CODE,
				Options = new QrCodeEncodingOptions()
				{
					Width = Width,
					Height = Height
				}
			};

			PixelData QrCode = Writer.Write(Text);
			Width = QrCode.Width;
			Height = QrCode.Height;

			int Size = Width * Height << 2;

			using (SKData Data = SKData.Create(Size))
			{
				Marshal.Copy(QrCode.Pixels, 0, Data.Data, Size);

				using (SKImage Result = SKImage.FromPixels(new SKImageInfo(Width, Height, SKColorType.Bgra8888), Data, Width << 2))
				{
					using (SKData Encoded = Result.Encode(SKEncodedImageFormat.Png, 100))
					{
						return Encoded.ToArray();
					}
				}
			}
		}
	}
}
