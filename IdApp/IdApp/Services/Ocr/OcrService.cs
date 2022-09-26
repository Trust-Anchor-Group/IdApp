using IdApp.Cv;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tesseract;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Ocr
{
	/// <summary>
	/// Optical Character Recognition (OCR) Service.
	/// </summary>
	[Singleton]
	public class OcrService : ServiceReferences, IOcrService
	{
		private ITesseractApi api;

		/// <summary>
		/// Optical Character Recognition (OCR) Service.
		/// </summary>
		public OcrService()
			: base()
		{
		}

		/// <summary>
		/// Method called to register the platform-dependant Tesseract API interface.
		/// </summary>
		/// <param name="Api">Tesseract API Reference</param>
		public void RegisterApi(ITesseractApi Api)
		{
			this.api = Api;
		}

		/// <summary>
		/// Tesseract API reference.
		/// </summary>
		public ITesseractApi Api => this.api;

		/// <summary>
		/// If API is created
		/// </summary>
		public bool Created => this.api is not null;

		/// <summary>
		/// If API is initialized
		/// </summary>
		public bool Initialized => this.api?.Initialized ?? false;

		/// <summary>
		/// Initializes API
		/// </summary>
		/// <returns>If API was successfully initialized.</returns>
		public async Task<bool> Initialize()
		{
			if (!await this.api.Init("eng"))
				return false;

			this.api.SetPageSegmentationMode(PageSegmentationMode.SingleBlock);

			return true;
		}

		/// <summary>
		/// Processes an image and tries to extract strings of characters from it.
		/// </summary>
		/// <param name="Image">Pre-processed image.</param>
		/// <returns>Any lines of text found.</returns>
		public async Task<string[]> ProcessImage(IMatrix Image)
		{
			byte[] Png = Bitmaps.EncodeAsPng(Image);
			if (!await this.api.SetImage(Png))
				return new string[0];

			List<string> Result = new();
			string s;

			foreach (Result R in this.api.Results(PageIteratorLevel.Textline))
			{
				s = R.Text.Trim();
				if (!string.IsNullOrEmpty(s))
					Result.Add(s);
			}

			return Result.ToArray();
		}

	}
}
