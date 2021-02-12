using IdApp.Navigation;
using IdApp.Views;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;

namespace IdApp
{
    /// <summary>
    /// Helper class to perform scanning of QR Codes by displaying the UI and handling async results.
    /// </summary>
    public static class QrCode
    {
        private static TaskCompletionSource<string> qrCodeScanned;

        /// <summary>
        /// Navigates to the Scan QR Code Page, waits for scan to comlete, and returns the result.
        /// </summary>
        /// <param name="navigationService">The navigation service to use for page navigation.</param>
        /// <param name="commandName">The localized name of the command to display when scanning.</param>
        /// <returns></returns>
        public static Task<string> ScanQrCode(INavigationService navigationService, string commandName)
        {
            _ = navigationService.GoToAsync(nameof(ScanQrCodePage), new ScanQrCodeNavigationArgs(commandName));
            qrCodeScanned = new TaskCompletionSource<string>();
            return qrCodeScanned.Task;
        }

        /// <summary>
        /// Tries to set the Scan QR Code result and close the scan page.
        /// </summary>
        /// <param name="navigationService">The navigation service to use for page navigation.</param>
        /// <param name="uiDispatcher">The current UI Dispatcher to use for marshalling back to the main thread.</param>
        /// <param name="code">The code to set.</param>
        public static void TrySetResultAndClosePage(INavigationService navigationService, IUiDispatcher uiDispatcher, string code)
        {
            if (!string.IsNullOrWhiteSpace(code) && qrCodeScanned != null)
            {
                qrCodeScanned.TrySetResult(code);
                qrCodeScanned = null;
            }
            uiDispatcher.BeginInvokeOnMainThread(async () => await navigationService.GoBackAsync());
        }
    }
}