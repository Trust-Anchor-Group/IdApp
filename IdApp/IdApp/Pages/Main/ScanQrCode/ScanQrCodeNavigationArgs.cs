using IdApp.Services.Navigation;
using System;
using System.Threading.Tasks;

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
		/// <param name="CommandName">The command name (localized) to display.</param>
		public ScanQrCodeNavigationArgs(string CommandName)
        {
            this.CommandName = CommandName;
			this.QrCodeScanned = new TaskCompletionSource<string>();
		}

        /// <summary>
        /// The command name (localized) to display.
        /// </summary>
        public string CommandName { get; }

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		public TaskCompletionSource<string> QrCodeScanned { get; internal set; }
	}
}
