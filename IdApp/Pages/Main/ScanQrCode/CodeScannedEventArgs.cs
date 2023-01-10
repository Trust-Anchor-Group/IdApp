using System;

namespace IdApp.Pages.Main.ScanQrCode
{
    /// <summary>
    /// An event args class that holds dat about the currently scanned QR code.
    /// </summary>
    public sealed class CodeScannedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="CodeScannedEventArgs"/> class.
        /// </summary>
        /// <param name="Url">The URL in a scanned QR code</param>
        public CodeScannedEventArgs(string Url)
        {
            this.Url = Url;
        }

        /// <summary>
        /// The scanned QR code URL
        /// </summary>
        public string Url { get; }
    }
}