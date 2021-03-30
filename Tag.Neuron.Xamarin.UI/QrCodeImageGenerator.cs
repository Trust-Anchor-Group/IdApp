using System.IO;
using SkiaSharp;
using Waher.Content.QR;
using Waher.Content.QR.Encoding;

namespace Tag.Neuron.Xamarin.UI
{
    /// <summary>
    /// This class can generate QR Code images in two formats: Jpeg and Png.
    /// </summary>
    public static class QrCodeImageGenerator
    {
        private static readonly QrEncoder encoder = new QrEncoder();

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

        private static byte[] Generate(string Text, int Width, int Height, SKEncodedImageFormat Format)
        {
            QrMatrix M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
            byte[] Rgba = M.ToRGBA(Width, Height);

            using (SKData Unencoded = SKData.Create(new MemoryStream(Rgba)))
            {
                using (SKImage Bitmap = SKImage.FromPixels(new SKImageInfo(Width, Height, SKColorType.Rgba8888), Unencoded, Width * 4))
                {
                    using (SKData Encoded = Bitmap.Encode(Format, 100))
                    {
                        return Encoded.ToArray();
                    }
                }
            }
        }
    }
}
