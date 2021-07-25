using System;
using System.Threading.Tasks;
using IdApp.Pages.Main.ScanQrCode;
using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Essentials;

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
        /// Scans a QR Code, and depending on the actual result, takes different actions. This typically means navigating to an appropriate page.
        /// </summary>
        /// <param name="logService">The log service to use for logging.</param>
        /// <param name="neuronService">The Neuron service for XMPP access</param>
        /// <param name="navigationService">The navigation service for page navigation.</param>
        /// <param name="uiDispatcher">The ui dispatcher for main thread access.</param>
        /// <param name="contractOrchestratorService">The contract orchestrator service.</param>
        /// <param name="thingRegistryOrchestratorService">The thing registry orchestrator service.</param>
        /// <param name="eDalerOrchestratorService">eDaler orchestrator service.</param>
        public static async Task ScanQrCodeAndHandleResult(
            ILogService logService,
            INeuronService neuronService,
            INavigationService navigationService,
            IUiDispatcher uiDispatcher,
            IContractOrchestratorService contractOrchestratorService,
            IThingRegistryOrchestratorService thingRegistryOrchestratorService,
            IEDalerOrchestratorService eDalerOrchestratorService)
        {
            string decodedText = await IdApp.QrCode.ScanQrCode(navigationService, AppResources.Open);

            if (string.IsNullOrWhiteSpace(decodedText))
                return;

            try
            {
                if (!Uri.TryCreate(decodedText, UriKind.Absolute, out Uri uri))
                {
                    await uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.CodeNotRecognized);
                    return;
                }

                switch (uri.Scheme.ToLower())
                {
                    case Constants.UriSchemes.UriSchemeIotId:
                        string legalId = Constants.UriSchemes.GetCode(decodedText);
                        await contractOrchestratorService.OpenLegalIdentity(legalId, AppResources.ScannedQrCode);
                        break;

                    case Constants.UriSchemes.UriSchemeIotSc:
                        string contractId = Constants.UriSchemes.GetCode(decodedText);
                        await contractOrchestratorService.OpenContract(contractId, AppResources.ScannedQrCode);
                        break;

                    case Constants.UriSchemes.UriSchemeIotDisco:
                        if (neuronService.ThingRegistry.IsIoTDiscoClaimURI(decodedText))
                            await thingRegistryOrchestratorService.OpenClaimDevice(decodedText);
                        else if (neuronService.ThingRegistry.IsIoTDiscoSearchURI(decodedText))
                            await thingRegistryOrchestratorService.OpenSearchDevices(decodedText);
                        else
                            await uiDispatcher.DisplayAlert(AppResources.ErrorTitle, $"{AppResources.InvalidIoTDiscoveryCode}{Environment.NewLine}{Environment.NewLine}{decodedText}");
                        break;

                    case Constants.UriSchemes.UriSchemeTagSign:
                        string request = Constants.UriSchemes.GetCode(decodedText);
                        await contractOrchestratorService.TagSignature(request);
                        break;

                    case Constants.UriSchemes.UriSchemeEDaler:
                        await eDalerOrchestratorService.OpenEDalerUri(decodedText);
                        break;

                    default:
                        if (!await Launcher.TryOpenAsync(uri))
                            await uiDispatcher.DisplayAlert(AppResources.ErrorTitle, $"{AppResources.QrCodeNotUnderstood}{Environment.NewLine}{Environment.NewLine}{decodedText}");
                        break;
                }
            }
            catch (Exception ex)
            {
                logService.LogException(ex);
                await uiDispatcher.DisplayAlert(ex);
            }

        }

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
        /// <returns>Decoded string</returns>
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