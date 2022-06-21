using System;
using System.IO;
using System.Threading.Tasks;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Main.ScanQrCode;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Xmpp;
using IdApp.Services.ThingRegistries;
using IdApp.Services.Wallet;
using SkiaSharp;
using Waher.Content.QR;
using Waher.Content.QR.Encoding;
using Xamarin.Essentials;
using IdApp.Resx;
using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;

namespace IdApp.Services.UI.QR
{
	/// <summary>
	/// Helper class to perform scanning of QR Codes by displaying the UI and handling async results.
	/// </summary>
	public static class QrCode
	{
		private static readonly QrEncoder encoder = new();
		private static TaskCompletionSource<string> qrCodeScanned;
		private static Func<string, Task> callback;

		/// <summary>
		/// Scans a QR Code, and depending on the actual result, takes different actions. 
		/// This typically means navigating to an appropriate page.
		/// </summary>
		public static async Task ScanQrCodeAndHandleResult()
		{
			string Url = await QrCode.ScanQrCode(App.Instantiate<INavigationService>(), AppResources.Open);
			if (string.IsNullOrWhiteSpace(Url))
				return;

			await OpenUrl(Url);
		}

		/// <summary>
		/// Scans a QR Code, and depending on the actual result, takes different actions. 
		/// This typically means navigating to an appropriate page.
		/// </summary>
		/// <param name="Url">URL to open.</param>
		/// <returns>If URL was handled.</returns>
		public static async Task<bool> OpenUrl(string Url)
		{
			ILogService LogService = App.Instantiate<ILogService>();
			IUiSerializer UiSerializer = App.Instantiate<IUiSerializer>();
			IXmppService XmppService = App.Instantiate<IXmppService>();
			IContractOrchestratorService ContractOrchestratorService = App.Instantiate<IContractOrchestratorService>();
			IThingRegistryOrchestratorService ThingRegistryOrchestratorService = App.Instantiate<IThingRegistryOrchestratorService>();
			INeuroWalletOrchestratorService EDalerOrchestratorService = App.Instantiate<INeuroWalletOrchestratorService>();

			try
			{
				if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri uri))
				{
					await UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.CodeNotRecognized);
					return false;
				}

				switch (uri.Scheme.ToLower())
				{
					case Constants.UriSchemes.UriSchemeIotId:
						string legalId = Constants.UriSchemes.RemoveScheme(Url);
						await ContractOrchestratorService.OpenLegalIdentity(legalId, AppResources.ScannedQrCode);
						return true;

					case Constants.UriSchemes.UriSchemeIotSc:
						Dictionary<string, object> Parameters = new();

						string contractId = Constants.UriSchemes.RemoveScheme(Url);
						int i = contractId.IndexOf('?');

						if (i > 0)
						{
							NameValueCollection QueryParameters = HttpUtility.ParseQueryString(contractId[i..]);

							foreach (string Key in QueryParameters.AllKeys)
								Parameters[Key] = QueryParameters[Key];

							contractId = contractId.Substring(0, i);
						}

						await ContractOrchestratorService.OpenContract(contractId, AppResources.ScannedQrCode, Parameters);
						return true;

					case Constants.UriSchemes.UriSchemeIotDisco:
						if (XmppService.ThingRegistry.IsIoTDiscoClaimURI(Url))
							await ThingRegistryOrchestratorService.OpenClaimDevice(Url);
						else if (XmppService.ThingRegistry.IsIoTDiscoSearchURI(Url))
							await ThingRegistryOrchestratorService.OpenSearchDevices(Url);
						else if (XmppService.ThingRegistry.IsIoTDiscoDirectURI(Url))
							await ThingRegistryOrchestratorService.OpenDeviceReference(Url);
						else
						{
							await UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidIoTDiscoveryCode + Environment.NewLine + Environment.NewLine + Url);
							return false;
						}
						return true;

					case Constants.UriSchemes.UriSchemeTagSign:
						string request = Constants.UriSchemes.RemoveScheme(Url);
						await ContractOrchestratorService.TagSignature(request);
						return true;

					case Constants.UriSchemes.UriSchemeEDaler:
						await EDalerOrchestratorService.OpenEDalerUri(Url);
						return true;

					case Constants.UriSchemes.UriSchemeNeuroFeature:
						await EDalerOrchestratorService.OpenNeuroFeatureUri(Url);
						return true;

					case Constants.UriSchemes.UriSchemeOnboarding:
						await UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ThisCodeCannotBeClaimedAtThisTime);
						return false;

					case Constants.UriSchemes.UriSchemeXmpp:
						return await ChatViewModel.ProcessXmppUri(Url);

					default:
						if (await Launcher.TryOpenAsync(uri))
							return true;
						else
						{
							await UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.QrCodeNotUnderstood + Environment.NewLine + Environment.NewLine + Url);
							return false;
						}
				}
			}
			catch (Exception ex)
			{
				LogService.LogException(ex);
				await UiSerializer.DisplayAlert(ex);
				return false;
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
		/// <param name="uiSerializer">The current UI Dispatcher to use for marshalling back to the main thread.</param>
		/// <param name="Url">The URL to set.</param>
		internal static void TrySetResultAndClosePage(INavigationService navigationService, IUiSerializer uiSerializer, string Url)
		{
			uiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if (!(callback is null))
				{
					await callback(Url);
					callback = null;
				}

				await navigationService.GoBackAsync();

				if (!string.IsNullOrWhiteSpace(Url) && !(qrCodeScanned is null))
				{
					qrCodeScanned.TrySetResult(Url.Trim());
					qrCodeScanned = null;
				}
			});
		}

		/// <summary>
		/// Generates a QR Code png image with the specified width and height.
		/// </summary>
		/// <param name="text">The QR Code</param>
		/// <param name="width">Required image width.</param>
		/// <param name="height">Required image height.</param>
		/// <returns>Binary encoding of PNG</returns>
		public static byte[] GeneratePng(string text, int width, int height)
		{
			return Generate(text, width, height, SKEncodedImageFormat.Png);
		}

		/// <summary>
		/// Generates a QR Code jpeg image with the specified width and height.
		/// </summary>
		/// <param name="text">The QR Code</param>
		/// <param name="width">Required image width.</param>
		/// <param name="height">Required image height.</param>
		/// <returns>Binary encoding of JPG</returns>
		public static byte[] GenerateJpg(string text, int width, int height)
		{
			return Generate(text, width, height, SKEncodedImageFormat.Jpeg);
		}

		private static byte[] Generate(string Text, int Width, int Height, SKEncodedImageFormat Format)
		{
			QrMatrix M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
			byte[] Rgba = M.ToRGBA(Width, Height);

			using SKData Unencoded = SKData.Create(new MemoryStream(Rgba));
			using SKImage Bitmap = SKImage.FromPixels(new SKImageInfo(Width, Height, SKColorType.Rgba8888), Unencoded, Width * 4);
			using SKData Encoded = Bitmap.Encode(Format, 100);

			return Encoded.ToArray();
		}
	}
}
