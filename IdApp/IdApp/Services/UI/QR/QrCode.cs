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
using Waher.Runtime.Inventory;
using IdApp.Links;

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
				if (!System.Uri.TryCreate(Url, UriKind.Absolute, out Uri Uri))
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["CodeNotRecognized"]);
					return false;
				}

				ILinkOpener Opener = Types.FindBest<ILinkOpener, Uri>(Uri);
				if (Opener is null)
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["CodeNotRecognized"]);
					return false;
				}

				if (!await Opener.TryOpenLink(Uri))
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToOpenLink"] + " " + Uri.ToString());
					return false;
				}

				return true;
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

			if (Text.StartsWith("tagsign:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, signatureCode.ColorFunction, true);
			}
			else if (Text.StartsWith("iotid:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, userCode.ColorFunction, true);
			}
			else if (Text.StartsWith("edaler:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, eDalerCode.ColorFunction, true);
			}
			else if (Text.StartsWith("obinfo:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, onboardingCode.ColorFunction, true);
			}
			else if (Text.StartsWith("aes256:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, aes256Code.ColorFunction, true);
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

		private readonly static CustomColoring eDalerCode = new(
			"M24.6263 16.4401C23.3663 16.4401 22.1063 16.4401 20.8463 16.4401C20.7206 16.44 20.5948 16.4401 20.4395 16.4401V51.4389H20.9223C24.8438 51.4389 28.7656 51.4428 32.6871 51.4358C33.5212 51.4344 34.3558 51.4085 35.189 51.3681C37.4651 51.2581 39.6652 50.7828 41.772 49.9128C45.1668 48.511 47.8677 46.2772 49.6567 43.0516C52.1445 38.5658 52.5325 33.7874 51.288 28.8799C50.035 23.9385 47.0312 20.3848 42.3972 18.2388C39.5603 16.9252 36.548 16.4476 33.4461 16.4435C31.2191 16.4405 28.9924 16.44 26.7654 16.44C26.0525 16.44 25.3391 16.44 24.6263 16.4401ZM29.0057 23.7511C29.0906 23.7458 29.1635 23.7367 29.2363 23.7376C29.766 23.7444 30.2963 23.7432 30.8267 23.742C32.0051 23.7392 33.1835 23.7365 34.3556 23.8212C38.7074 24.1359 42.1845 26.6075 43.0292 31.4776C43.4386 33.8372 43.3465 36.1684 42.4712 38.415C41.2914 41.4435 38.9755 43.1121 35.8589 43.7698C34.4877 44.0592 33.0964 44.0526 31.7052 44.0495C30.8097 44.0475 29.914 44.0495 28.989 44.0495V37.1036H40.4938V30.4922H29.0057V23.7514V23.7511Z",
			67, 67, SKColors.Goldenrod, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.SaddleBrown, SKColors.Sienna, SKColors.White,
			SKColors.SaddleBrown, SKColors.Sienna, SKColors.White);

		private readonly static CustomColoring aes256Code = new(
			"M44.2122 28.1067H42.5456V24.7733C42.5456 20.1733 38.8122 16.44 34.2122 16.44C29.6122 16.44 25.8789 20.1733 25.8789 24.7733V28.1067H24.2122C22.3789 28.1067 20.8789 29.6067 20.8789 31.44V48.1067C20.8789 49.94 22.3789 51.44 24.2122 51.44H44.2122C46.0456 51.44 47.5456 49.94 47.5456 48.1067V31.44C47.5456 29.6067 46.0456 28.1067 44.2122 28.1067ZM34.2122 43.1067C32.3789 43.1067 30.8789 41.6067 30.8789 39.7733C30.8789 37.94 32.3789 36.44 34.2122 36.44C36.0456 36.44 37.5456 37.94 37.5456 39.7733C37.5456 41.6067 36.0456 43.1067 34.2122 43.1067ZM29.2122 28.1067V24.7733C29.2122 22.0067 31.4456 19.7733 34.2122 19.7733C36.9789 19.7733 39.2122 22.0067 39.2122 24.7733V28.1067H29.2122Z",
			68, 67, SKColors.SlateGray, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.DarkSlateGray, SKColors.SlateGray, SKColors.White,
			SKColors.DarkSlateGray, SKColors.SlateGray, SKColors.White);

		private readonly static CustomColoring signatureCode = new(
			"M34.1818 29.2727C32.6909 25.0364 28.6545 22 23.9091 22C17.8909 22 13 26.8909 13 32.9091C13 38.9273 17.8909 43.8182 23.9091 43.8182C28.6545 43.8182 32.6909 40.7818 34.1818 36.5455H42.0909V43.8182H49.3636V36.5455H53V29.2727H34.1818ZM23.9091 36.5455C21.9091 36.5455 20.2727 34.9091 20.2727 32.9091C20.2727 30.9091 21.9091 29.2727 23.9091 29.2727C25.9091 29.2727 27.5455 30.9091 27.5455 32.9091C27.5455 34.9091 25.9091 36.5455 23.9091 36.5455Z",
			67, 67, SKColors.SlateGray, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.DarkSlateGray, SKColors.SlateGray, SKColors.White,
			SKColors.DarkSlateGray, SKColors.SlateGray, SKColors.White);

	}
}
