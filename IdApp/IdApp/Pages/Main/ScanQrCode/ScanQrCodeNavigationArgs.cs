using IdApp.Services;
using IdApp.Services.Navigation;

namespace IdApp.Pages.Main.ScanQrCode
{
    /// <summary>
    /// Holds navigation parameters specific to views scanning a QR code.
    /// </summary>

    public class ScanQrCodeNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class.
        /// </summary>
        public ScanQrCodeNavigationArgs() { }

        /// <summary>
        /// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class.
        /// </summary>
        /// <param name="commandName">The command name (localized) to display.</param>
        public ScanQrCodeNavigationArgs(string commandName)
        {
            this.CommandName = commandName;
        }

        /// <summary>
        /// The command name (localized) to display.
        /// </summary>
        public string CommandName { get; }
    }
}