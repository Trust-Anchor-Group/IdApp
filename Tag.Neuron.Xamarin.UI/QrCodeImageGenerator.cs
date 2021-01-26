using SkiaSharp;
using SkiaSharp.QrCode;

namespace Tag.Neuron.Xamarin.UI
{
    public static class QrCodeImageGenerator
    {
        public static byte[] GeneratePng(string text, int width, int height)
        {
            return Generate(text, width, height, SKEncodedImageFormat.Png);
        }

        public static byte[] GenerateJpg(string text, int width, int height)
        {
            return Generate(text, width, height, SKEncodedImageFormat.Jpeg);
        }

        private static byte[] Generate(string text, int width, int height, SKEncodedImageFormat format)
        {
            using (QRCodeGenerator generator = new QRCodeGenerator())
            {
                QRCodeData qr = generator.CreateQrCode(text, ECCLevel.H);
                using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    surface.Canvas.Render(qr, SKRect.Create(0.0f, 0.0f, width, height), SKColors.White, SKColors.Black);

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(format, 100))
                    {
                        return data.ToArray();
                    }
                }
            }
        }
    }
}
