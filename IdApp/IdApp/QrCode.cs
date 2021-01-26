using IdApp.Navigation;
using IdApp.Views;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;

namespace IdApp
{
    public static class QrCode
    {
        private static TaskCompletionSource<string> qrCodeScanned;

        public static Task<string> ScanQrCode(INavigationService navigationService, string commandName)
        {
            _ = navigationService.GoToAsync(nameof(ScanQrCodePage), new ScanQrCodeNavigationArgs(commandName));
            qrCodeScanned = new TaskCompletionSource<string>();
            return qrCodeScanned.Task;
        }

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