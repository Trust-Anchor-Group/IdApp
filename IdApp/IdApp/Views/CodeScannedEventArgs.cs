using System;

namespace IdApp.Views
{
    /// <summary>
    /// An event args class that holds dat about the currently scanned QR code.
    /// </summary>
    public sealed class CodeScannedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="CodeScannedEventArgs"/> class.
        /// </summary>
        /// <param name="code">The scanned QR code</param>
        public CodeScannedEventArgs(string code)
        {
            Code = code;
        }

        /// <summary>
        /// The scanned QR code
        /// </summary>
        public string Code { get; }
    }
}