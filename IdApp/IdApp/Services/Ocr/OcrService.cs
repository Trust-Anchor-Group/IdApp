﻿using IdApp.Cv;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Ocr
{
	/// <summary>
	/// Optical Character Recognition (OCR) Service.
	/// </summary>
	[Singleton]
	public class OcrService : ServiceReferences, IOcrService
	{
		/// <summary>
		/// Optical Character Recognition (OCR) Service.
		/// </summary>
		public OcrService()
			: base()
		{
		}

		/// <summary>
		/// Processes an image and tries to extract strings of characters from it.
		/// </summary>
		/// <param name="Image">Pre-processed image.</param>
		/// <param name="Language">Expected language on text in image.</param>
		/// <param name="PageSegmentationMode">Optional page segmentationmode.</param>
		/// <returns>Any lines of text found.</returns>
		public async Task<string[]> ProcessImage(IMatrix Image, string Language, PageSegmentationMode? PageSegmentationMode)
		{
			byte[] Png = Bitmaps.EncodeAsPng(Image);
			string Token = await this.XmppService.GetApiToken(60);

			Uri Uri = new("https://" + this.TagProfile.Domain + "/Tesseract/Api");
			using HttpClient HttpClient = new();
			using HttpRequestMessage Request = new()
			{
				RequestUri = Uri,
				Method = HttpMethod.Post,
				Content = new ByteArrayContent(Png)
			};

			Request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
			Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

			if (!string.IsNullOrEmpty(Language))
				Request.Headers.Add("X-LANGUAGE", Language);

			if (PageSegmentationMode.HasValue)
				Request.Headers.Add("X-PSM", PageSegmentationMode.Value.ToString());

			HttpResponseMessage Response = await HttpClient.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			byte[] Bin = await Response.Content.ReadAsByteArrayAsync();
			string ContentType = Response.Content.Headers.ContentType.ToString();
			object Obj = await InternetContent.DecodeAsync(ContentType, Bin, Uri);

			if (Obj is not string Text)
				throw new Exception("Unexpected response.");

			if (string.IsNullOrEmpty(Text))
				return new string[0];
			else
				return Text.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);
		}

	}
}
