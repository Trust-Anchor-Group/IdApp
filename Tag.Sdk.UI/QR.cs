using SkiaSharp;
using SkiaSharp.QrCode;

namespace Tag.Sdk.UI
{
    public static class QR
    {
        public static byte[] GenerateCodePng(string text, int width, int height)
        {
            using (QRCodeGenerator generator = new QRCodeGenerator())
            {
                QRCodeData qr = generator.CreateQrCode(text, ECCLevel.H);
                using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    surface.Canvas.Render(qr, SKRect.Create(0.0f, 0.0f, width, height), SKColors.White, SKColors.Black);

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        return data.ToArray();
                    }
                }
            }
        }
    }
}
