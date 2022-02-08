using IdApp.Cv;
using System.Threading.Tasks;
using Tesseract;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Ocr
{
	/// <summary>
	/// Interface for the Optical Character Recognition (OCR) Service.
	/// </summary>
	[DefaultImplementation(typeof(OcrService))]
	public interface IOcrService
	{
		/// <summary>
		/// Method called to register the platform-dependant Tesseract API interface.
		/// </summary>
		/// <param name="Api">Tesseract API Reference</param>
		void RegisterApi(ITesseractApi Api);

		/// <summary>
		/// If API is created
		/// </summary>
		bool Created { get; }

		/// <summary>
		/// If API is initialized
		/// </summary>
		bool Initialized { get; }

		/// <summary>
		/// Initializes API
		/// </summary>
		/// <returns>If API was successfully initialized.</returns>
		Task<bool> Initialize();

		/// <summary>
		/// Processes an image and tries to extract strings of characters from it.
		/// </summary>
		/// <param name="Image">Pre-processed image.</param>
		/// <returns>Any lines of text found.</returns>
		Task<string[]> ProcessImage(IMatrix Image);

	}
}
