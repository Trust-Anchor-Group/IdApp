using SkiaSharp;
using SkiaSharp.QrCode;

namespace Tag.Neuron.Xamarin.UI
{
    /// <summary>
    /// This class can generate QR Code images in two formats: Jpeg and Png.
    /// </summary>
    public static class QrCodeImageGenerator
    {
        /// <summary>
        /// Generates a QR Code png image with the specified width and height.
        /// </summary>
        /// <param name="text">The QR Code</param>
        /// <param name="width">Required image width.</param>
        /// <param name="height">Required image height.</param>
        /// <returns></returns>
        public static byte[] GeneratePng(string text, int width, int height)
        {
            return Generate(text, width, height, SKEncodedImageFormat.Png);
        }

        /// <summary>
        /// Generates a QR Code jpeg image with the specified width and height.
        /// </summary>
        /// <param name="text">The QR Code</param>
        /// <param name="width">Required image width.</param>
        /// <param name="height">Required image height.</param>
        /// <returns></returns>
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
