using System.IO;

namespace XamarinApp.Extensions
{
    public static class StreamExtensions
    {
        public static void Reset(this Stream stream)
        {
            if (stream != null && stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            else if (stream != null)
            {
                stream.Position = 0;
            }
        }
    }
}