using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using XamarinApp.Navigation;
using XamarinApp.Views;

namespace XamarinApp
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