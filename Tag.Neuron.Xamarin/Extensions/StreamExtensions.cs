using System.IO;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Convenience method for resetting a stream to position = 0.
        /// </summary>
        /// <param name="stream">The stream to reset.</param>
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