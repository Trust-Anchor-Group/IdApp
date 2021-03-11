﻿using System;
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
        private static Func<string, Task> callback;

        /// <summary>
        /// Navigates to the Scan QR Code Page, waits for scan to complete, and returns the result.
        /// This is seemingly simple, but performs several operations, namely:
        /// <list type="bullet">
        /// <item>
        /// <description>Display the <see cref="ScanQrCodePage"/></description>
        /// </item>
        /// <item>
        /// <description>Wait for the user to scan a QR code or enter it manually, or cancel.</description>
        /// </item>
        /// <item>
        /// <description>Navigate back to the calling page.</description>
        /// </item>
        /// </list>
        /// In order to handle processing in the correct order, you may need to use the <c>action</c> parameter. It is provided
        /// to do additional processing <em>before</em> the <see cref="ScanQrCodePage"/> is navigated away from.
        /// </summary>
        /// <param name="navigationService">The navigation service to use for page navigation.</param>
        /// <param name="commandName">The localized name of the command to display when scanning.</param>
        /// <param name="action">The asynchronous action to invoke right after a QR Code has been scanned, but before the Scan Page closes.</param>
        /// <returns></returns>
        public static Task<string> ScanQrCode(INavigationService navigationService, string commandName, Func<string, Task> action = null)
        {
            callback = action;
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
            uiDispatcher.BeginInvokeOnMainThread(async () =>
            {
                if (callback != null)
                {
                    await callback(code);
                }
                callback = null;
                await navigationService.GoBackAsync();
                if (!string.IsNullOrWhiteSpace(code) && !(qrCodeScanned is null))
                {
                    qrCodeScanned.TrySetResult(code.Trim());
                    qrCodeScanned = null;
                }
            });
        }
    }
}