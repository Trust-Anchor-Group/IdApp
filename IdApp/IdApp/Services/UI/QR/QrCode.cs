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
using Waher.Persistence;
using Waher.Security.JWT;

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

						Dictionary<CaseInsensitiveString, object> Parameters = new();

						string contractId = Constants.UriSchemes.RemoveScheme(Url);
						int i = contractId.IndexOf('?');

						if (i > 0)
						{
							NameValueCollection QueryParameters = HttpUtility.ParseQueryString(contractId[i..]);

							foreach (string Key in QueryParameters.AllKeys)
								Parameters[Key] = QueryParameters[Key];

							contractId = contractId[..i];
						}

						await Services.ContractOrchestratorService.OpenContract(contractId, LocalizationResourceManager.Current["ScannedQrCode"], Parameters);
						return true;

					case Constants.UriSchemes.UriSchemeIotDisco:
						if (Services.XmppService.IsIoTDiscoClaimURI(Url))
							await Services.ThingRegistryOrchestratorService.OpenClaimDevice(Url);
						else if (Services.XmppService.IsIoTDiscoSearchURI(Url))
							await Services.ThingRegistryOrchestratorService.OpenSearchDevices(Url);
						else if (Services.XmppService.IsIoTDiscoDirectURI(Url))
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

					case Constants.UriSchemes.UriSchemeTagIdApp:
						string Token = Constants.UriSchemes.RemoveScheme(Url);
						JwtToken Parsed = Services.CryptoService.ParseAndValidateJwtToken(Token);
						if (Parsed is null)
							return false;

						if (!Parsed.TryGetClaim("cmd", out object Obj) || Obj is not string Command ||
							!Parsed.TryGetClaim(JwtClaims.ClientId, out Obj) || Obj is not string ClientId ||
							ClientId != Services.CryptoService.DeviceID ||
							!Parsed.TryGetClaim(JwtClaims.Issuer, out Obj) || Obj is not string Issuer ||
							Issuer != Services.CryptoService.DeviceID ||
							!Parsed.TryGetClaim(JwtClaims.Subject, out Obj) || Obj is not string Subject ||
							Subject != Services.XmppService.BareJid)
						{
							return false;
						}

						switch (Command)
						{
							case "bes":  // Buy eDaler Successful
								if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId ||
									!Parsed.TryGetClaim("amt", out object Amount) ||
									!Parsed.TryGetClaim("cur", out Obj) || Obj is not string Currency)
								{
									return false;
								}

								decimal AmountDec;

								try
								{
									AmountDec = Convert.ToDecimal(Amount);
								}
								catch (Exception)
								{
									return false;
								}

								Services.XmppService.BuyEDalerCompleted(TransactionId, AmountDec, Currency);
								return true;

							case "bef":  // Buy eDaler Failed
								if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId2)
									return false;

								Services.XmppService.BuyEDalerFailed(TransactionId2, LocalizationResourceManager.Current["PaymentFailed"]);
								return true;

							case "bec":  // Buy eDaler Cancelled
								if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId3)
									return false;

								Services.XmppService.BuyEDalerFailed(TransactionId3, LocalizationResourceManager.Current["PaymentCancelled"]);
								return true;

							case "ses":  // Sell eDaler Successful
								if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId4 ||
									!Parsed.TryGetClaim("amt", out Amount) ||
									!Parsed.TryGetClaim("cur", out Obj) || Obj is not string Currency4)
								{
									return false;
								}

								try
								{
									AmountDec = Convert.ToDecimal(Amount);
								}
								catch (Exception)
								{
									return false;
								}

								Services.XmppService.SellEDalerCompleted(TransactionId4, AmountDec, Currency4);
								return true;

							case "sef":  // Sell eDaler Failed
								if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId5)
									return false;

								Services.XmppService.SellEDalerFailed(TransactionId5, LocalizationResourceManager.Current["PaymentFailed"]);
								return true;

							case "sec":  // Sell eDaler Cancelled
								if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId6)
									return false;

								Services.XmppService.SellEDalerFailed(TransactionId6, LocalizationResourceManager.Current["PaymentCancelled"]);
								return true;

							default:
								return false;
						}

					default:
						if (await Launcher.TryOpenAsync(uri))
							return true;
						else
						{
							await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
								LocalizationResourceManager.Current["QrCodeNotUnderstood"] + Environment.NewLine +
								Environment.NewLine + Url);

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
		/// <param name="Action">
		/// The asynchronous action to invoke right after a QR Code has been scanned, but before the Scan Page closes.
		/// <para>
		/// <paramref name="Action"/> should not navigate and (!) should not post navigation using BeginInvokeOnMainThread or
		/// similar methods. Otherwise, trying to navigate back from the QR code page can actually navigate from the wrong page.
		/// </para>
		/// </param>
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
			QrMatrix M;
			byte[] Rgba;

			if (Text.StartsWith("iotid:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, userCode.ColorFunction, true);
			}
			else if (Text.StartsWith("obinfo:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, onboardingCode.ColorFunction, true);
			}
			else
			{
				M = encoder.GenerateMatrix(CorrectionLevel.L, Text);
				Rgba = M.ToRGBA(Width, Height);
			}

			using SKData Unencoded = SKData.Create(new MemoryStream(Rgba));
			using SKImage Bitmap = SKImage.FromPixels(new SKImageInfo(Width, Height, SKColorType.Rgba8888), Unencoded, Width * 4);
			using SKData Encoded = Bitmap.Encode(Format, 100);

			return Encoded.ToArray();
		}

		private readonly static CustomColoring userCode = new(
			"M128 21.3335C69.1202 21.3335 21.3335 69.1202 21.3335 128C21.3335 186.88 69.1202 234.667 128 234.667C186.88 234.667 234.667 186.88 234.667 128C234.667 69.1202 186.88 21.3335 128 21.3335ZM128 53.3335C145.707 53.3335 160 67.6268 160 85.3335C160 103.04 145.707 117.333 128 117.333C110.293 117.333 96.0002 103.04 96.0002 85.3335C96.0002 67.6268 110.293 53.3335 128 53.3335ZM128 204.8C101.333 204.8 77.7602 191.147 64.0002 170.453C64.3202 149.227 106.667 137.6 128 137.6C149.227 137.6 191.68 149.227 192 170.453C178.24 191.147 154.667 204.8 128 204.8Z",
			256, 256, SKColors.Red, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.DarkRed, SKColors.Red, SKColors.White,
			SKColors.DarkSlateGray, SKColors.SlateGray, SKColors.White);

		private readonly static CustomColoring onboardingCode = new(
			"M181.523 136.57H76.6327C68.3914 136.57 61.6484 143.313 61.6484 151.554V181.523C61.6484 189.764 68.3914 196.507 76.6327 196.507H181.523C189.764 196.507 196.507 189.764 196.507 181.523V151.554C196.507 143.313 189.764 136.57 181.523 136.57ZM91.617 181.523C83.3756 181.523 76.6327 174.78 76.6327 166.538C76.6327 158.297 83.3756 151.554 91.617 151.554C99.8583 151.554 106.601 158.297 106.601 166.538C106.601 174.78 99.8583 181.523 91.617 181.523ZM181.523 61.6484H76.6327C68.3914 61.6484 61.6484 68.3914 61.6484 76.6327V106.601C61.6484 114.843 68.3914 121.586 76.6327 121.586H181.523C189.764 121.586 196.507 114.843 196.507 106.601V76.6327C196.507 68.3914 189.764 61.6484 181.523 61.6484ZM91.617 106.601C83.3756 106.601 76.6327 99.8583 76.6327 91.617C76.6327 83.3756 83.3756 76.6327 91.617 76.6327C99.8583 76.6327 106.601 83.3756 106.601 91.617C106.601 99.8583 99.8583 106.601 91.617 106.601Z",
			256, 256, SKColors.Green, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.DarkGreen, SKColors.Green, SKColors.White,
			SKColors.DarkSlateGray, SKColors.SlateGray, SKColors.White);

	}
}
