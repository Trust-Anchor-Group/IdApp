using System.IO;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// An extensions class for <see cref="Stream"/>s.
    /// </summary>
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