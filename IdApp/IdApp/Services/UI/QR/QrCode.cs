using System;
using System.IO;
using System.Threading.Tasks;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Main.ScanQrCode;
using IdApp.Services.Navigation;
using SkiaSharp;
using Waher.Content.QR;
using Waher.Content.QR.Encoding;
using Xamarin.Essentials;
using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;
using IdApp.Services.Notification.Identities;
using IdApp.Services.Notification.Contracts;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.UI.QR
{
	/// <summary>
	/// Helper class to perform scanning of QR Codes by displaying the UI and handling async results.
	/// </summary>
	public static class QrCode
	{
		private static readonly QrEncoder encoder = new();

		/// <summary>
		/// Scans a QR Code, and depending on the actual result, takes different actions. 
		/// This typically means navigating to an appropriate page.
		/// </summary>
		public static async Task ScanQrCodeAndHandleResult(bool UseShellNavigationService = true)
		{
			string Url = await QrCode.ScanQrCode(LocalizationResourceManager.Current["Open"], Action: null, UseShellNavigationService: UseShellNavigationService);
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
			ServiceReferences Services = new();

			try
			{
				if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri uri))
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["CodeNotRecognized"]);
					return false;
				}

				switch (uri.Scheme.ToLower())
				{
					case Constants.UriSchemes.UriSchemeIotId:
						Services.NotificationService.ExpectEvent<IdentityResponseNotificationEvent>(DateTime.Now.AddMinutes(1));
						string legalId = Constants.UriSchemes.RemoveScheme(Url);
						await Services.ContractOrchestratorService.OpenLegalIdentity(legalId, LocalizationResourceManager.Current["ScannedQrCode"]);
						return true;

					case Constants.UriSchemes.UriSchemeIotSc:
						Services.NotificationService.ExpectEvent<ContractResponseNotificationEvent>(DateTime.Now.AddMinutes(1));

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

						await Services.ContractOrchestratorService.OpenContract(contractId, LocalizationResourceManager.Current["ScannedQrCode"], Parameters);
						return true;

					case Constants.UriSchemes.UriSchemeIotDisco:
						if (Services.XmppService.ThingRegistry.IsIoTDiscoClaimURI(Url))
							await Services.ThingRegistryOrchestratorService.OpenClaimDevice(Url);
						else if (Services.XmppService.ThingRegistry.IsIoTDiscoSearchURI(Url))
							await Services.ThingRegistryOrchestratorService.OpenSearchDevices(Url);
						else if (Services.XmppService.ThingRegistry.IsIoTDiscoDirectURI(Url))
							await Services.ThingRegistryOrchestratorService.OpenDeviceReference(Url);
						else
						{
							await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["InvalidIoTDiscoveryCode"] + Environment.NewLine + Environment.NewLine + Url);
							return false;
						}
						return true;

					case Constants.UriSchemes.UriSchemeTagSign:
						Services.NotificationService.ExpectEvent<RequestSignatureNotificationEvent>(DateTime.Now.AddMinutes(1));

						string request = Constants.UriSchemes.RemoveScheme(Url);
						await Services.ContractOrchestratorService.TagSignature(request);
						return true;

					case Constants.UriSchemes.UriSchemeEDaler:
						await Services.NeuroWalletOrchestratorService.OpenEDalerUri(Url);
						return true;

					case Constants.UriSchemes.UriSchemeNeuroFeature:
						await Services.NeuroWalletOrchestratorService.OpenNeuroFeatureUri(Url);
						return true;

					case Constants.UriSchemes.UriSchemeOnboarding:
						await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["ThisCodeCannotBeClaimedAtThisTime"]);
						return false;

					case Constants.UriSchemes.UriSchemeXmpp:
						return await ChatViewModel.ProcessXmppUri(Url);

					default:
						if (await Launcher.TryOpenAsync(uri))
							return true;
						else
						{
							await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["QrCodeNotUnderstood"] + Environment.NewLine + Environment.NewLine + Url);
							return false;
						}
				}
			}
			catch (Exception ex)
			{
				Services.LogService.LogException(ex);
				await Services.UiSerializer.DisplayAlert(ex);
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
		/// <param name="CommandName">The localized name of the command to display when scanning.</param>
		/// <param name="Action">The asynchronous action to invoke right after a QR Code has been scanned, but before the Scan Page closes.</param>
		/// <param name="UseShellNavigationService">A Boolean flag indicating if Shell navigation should be used or a simple <c>PushAsync</c>.</param>
		/// <returns>Decoded string</returns>
		public static Task<string> ScanQrCode(string CommandName, Func<string, Task> Action = null, bool UseShellNavigationService = true)
		{
			ScanQrCodeNavigationArgs NavigationArgs = new(CommandName, Action);
			if (UseShellNavigationService)
			{
				INavigationService NavigationService = App.Instantiate<INavigationService>();
				_ = NavigationService.GoToAsync(nameof(ScanQrCodePage), NavigationArgs);
			}
			else
			{
				_ = App.Current.MainPage.Navigation.PushAsync(new ScanQrCodePage(NavigationArgs));
			}

			return NavigationArgs.QrCodeScanned.Task;
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
