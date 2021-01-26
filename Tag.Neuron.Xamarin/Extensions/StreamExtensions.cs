using System.IO;

namespace Tag.Neuron.Xamarin.Extensions
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