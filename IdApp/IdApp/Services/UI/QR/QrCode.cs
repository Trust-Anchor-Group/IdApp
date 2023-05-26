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
			else if (Text.StartsWith("iotsc:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, contractCode.ColorFunction, true);
			}
			//else if (Text.StartsWith("iotdisco:", StringComparison.CurrentCultureIgnoreCase))
			//{
			//	M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
			//	Rgba = M.ToRGBA(Width, Height, thingsCode.ColorFunction, true);
			//}
			else if (Text.StartsWith("edaler:", StringComparison.CurrentCultureIgnoreCase))
			{
				M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
				Rgba = M.ToRGBA(Width, Height, eDalerCode.ColorFunction, true);
			}
			//else if (Text.StartsWith("nfeat:", StringComparison.CurrentCultureIgnoreCase))
			//{
			//	M = encoder.GenerateMatrix(CorrectionLevel.H, Text);
			//	Rgba = M.ToRGBA(Width, Height, tokenCode.ColorFunction, true);
			//}
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

		private readonly static CustomColoring contractCode = new(
			"M34.3809 61.44C49.5687 61.44 61.8809 49.1278 61.8809 33.94C61.8809 18.7522 49.5687 6.44 34.3809 6.44C19.193 6.44 6.88086 18.7522 6.88086 33.94C6.88086 49.1278 19.193 61.44 34.3809 61.44ZM27.0428 16.44H41.0432L51.5435 26.94V47.94C51.5435 49.865 49.9684 51.44 48.0434 51.44H27.0253C25.1002 51.44 23.5427 49.865 23.5427 47.94L23.5502 35.89H22.0197V45.1152C22.0197 45.9724 21.5061 46.7396 20.7122 47.0699L19.661 47.5068C19.6621 47.5273 19.6649 47.5473 19.6678 47.5673C19.6714 47.5927 19.675 47.618 19.675 47.6442C19.675 49.032 18.5405 50.1608 17.1458 50.1608C15.7512 50.1608 14.6167 49.032 14.6167 47.6442C14.6167 46.2565 15.7512 45.1276 17.1458 45.1276C17.8867 45.1276 18.5474 45.452 19.0105 45.9584L20.0628 45.5212C20.2273 45.4526 20.3336 45.2935 20.3336 45.1149L20.3336 35.8897H18.7837C18.4346 36.8641 17.5074 37.5675 16.41 37.5675C15.0153 37.5675 13.8809 36.4386 13.8809 35.0509C13.8809 33.6631 15.0153 32.5343 16.41 32.5343C17.5077 32.5343 18.4346 33.2376 18.7837 34.212H20.3336L20.3336 24.9868C20.3336 24.8082 20.2267 24.6485 20.0618 24.58L19.0099 24.1427C18.5471 24.6491 17.8864 24.9732 17.1455 24.9732C15.7509 24.9732 14.6164 23.8443 14.6164 22.4566C14.6164 21.0688 15.7509 19.94 17.1455 19.94C18.5402 19.94 19.6747 21.0688 19.6747 22.4566C19.6747 22.4832 19.6711 22.5087 19.6675 22.5342L19.6675 22.5342C19.6646 22.5543 19.6618 22.5743 19.6607 22.5949L20.7119 23.0322C21.5064 23.3625 22.0194 24.13 22.0194 24.9869L22.0197 34.212H23.5513L23.5602 19.94C23.5602 18.015 25.1177 16.44 27.0428 16.44ZM30.5429 44.44H44.5433V40.94H30.5429V44.44ZM30.5429 37.44H44.5433V33.94H30.5429V37.44ZM39.2931 19.065V28.69H48.9184L39.2931 19.065Z",
			68, 67, SKColors.RoyalBlue, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.Navy, SKColors.RoyalBlue, SKColors.White,
			SKColors.Navy, SKColors.RoyalBlue, SKColors.White);

		private readonly static CustomColoring thingsCode = new(
			"M33.5 61.88C48.6878 61.88 61 49.5678 61 34.38C61 19.1922 48.6878 6.88 33.5 6.88C18.3122 6.88 6 19.1922 6 34.38C6 49.5678 18.3122 61.88 33.5 61.88ZM39.0208 26.1716V23.0258H42.1666V19.88H20.1458C18.4156 19.88 17 21.2956 17 23.0258V48.1923C17 49.9225 18.4156 51.3381 20.1458 51.3381H42.1666V48.1923H35.8278C37.7625 46.7609 39.0208 44.4802 39.0208 41.9007V34.0361H23.2917V41.9007C23.2917 44.4802 24.5657 46.7609 26.4847 48.1923H20.1458V23.0258H23.2917V26.1716C23.2917 27.0367 23.9995 27.7445 24.8646 27.7445H37.4479C38.313 27.7445 39.0208 27.0367 39.0208 26.1716ZM31.1562 32.4632C32.0249 32.4632 32.7291 31.759 32.7291 30.8903C32.7291 30.0216 32.0249 29.3174 31.1562 29.3174C30.2875 29.3174 29.5833 30.0216 29.5833 30.8903C29.5833 31.759 30.2875 32.4632 31.1562 32.4632ZM44.6398 31.0793C44.0197 31.0241 43.553 30.496 43.553 29.8697C43.553 29.1329 44.1854 28.5619 44.9038 28.6417C51.2834 29.2864 56.3551 34.3581 56.9936 40.7315C57.0673 41.456 56.4902 42.0823 55.7656 42.0823C55.1393 42.0823 54.6113 41.6033 54.5499 40.9771C54.028 35.7519 49.865 31.5889 44.6398 31.0793ZM43.553 41.2042V39.3991C43.553 38.8219 44.1179 38.3921 44.6582 38.5517C45.8003 38.9201 46.6967 39.8166 47.059 40.9525C47.2371 41.5112 46.8011 42.07 46.224 42.07H44.4188C43.9398 42.07 43.553 41.6832 43.553 41.2042ZM44.5968 36.022C43.9951 35.9238 43.553 35.4203 43.553 34.8186C43.553 34.0695 44.1977 33.4739 44.9284 33.5906C48.5756 34.18 51.4553 37.0597 52.0386 40.7008C52.1614 41.4253 51.5597 42.0761 50.8229 42.0761H50.8106C50.2089 42.0761 49.7115 41.6279 49.6071 41.0323C49.1712 38.4719 47.1572 36.458 44.5968 36.022Z",
			67, 68, SKColors.MediumVioletRed, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.Crimson, SKColors.MediumVioletRed, SKColors.White,
			SKColors.Crimson, SKColors.MediumVioletRed, SKColors.White);

		private readonly static CustomColoring tokenCode = new(
			"M33.5 61C48.6878 61 61 48.6878 61 33.5C61 18.3122 48.6878 6 33.5 6C18.3122 6 6 18.3122 6 33.5C6 48.6878 18.3122 61 33.5 61ZM31.6724 16.4659C32.7687 15.8447 34.1209 15.8447 35.2172 16.4659L48.0076 23.5555L38.7437 28.7082C37.4098 27.3195 35.5278 26.4424 33.4448 26.4424C31.3618 26.4424 29.4798 27.3195 28.1459 28.7082L18.882 23.5737L31.6724 16.4659ZM33.4448 30.0968C31.4349 30.0968 29.7904 31.7413 29.7904 33.7512C29.7904 35.7612 31.4349 37.4056 33.4448 37.4056C35.4547 37.4056 37.0992 35.7612 37.0992 33.7512C37.0992 31.7413 35.4547 30.0968 33.4448 30.0968ZM26.3735 31.9058L17 26.6983V40.7311C17 42.065 17.7126 43.2892 18.882 43.9287L31.6176 51V40.8225C28.4565 40.0185 26.136 37.1498 26.136 33.7512C26.136 33.1117 26.2091 32.4905 26.3735 31.9058ZM35.272 40.8225V51L48.0076 43.9287C49.177 43.2892 49.8896 42.065 49.8896 40.7311V26.6983L40.5161 31.9058C40.6805 32.4905 40.7536 33.1117 40.7536 33.7512C40.7536 37.1498 38.433 40.0185 35.272 40.8225Z",
			67, 67, SKColors.Green, SKColors.White, SKColors.Black, SKColors.White,
			SKColors.DarkGreen, SKColors.Green, SKColors.White,
			SKColors.DarkGreen, SKColors.Green, SKColors.White);
	}
}
