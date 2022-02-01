using IdApp.Cv.Arithmetics;
using IdApp.Cv.Transformations.Thresholds;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 3x3 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<float> HighlightFeatures_3x3(this Matrix<float> M, float Threshold)
		{
			Matrix<float> Image1 = M.
				Dilate(Shape.Cross_3x3).
				Erode(Shape.Diamond_3x3);

			Matrix<float> Image2 = M.
				Dilate(Shape.X_3x3).
				Erode(3);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 5x5 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<float> HighlightFeatures_5x5(this Matrix<float> M, float Threshold)
		{
			Matrix<float> Image1 = M.
				Dilate(Shape.Cross_5x5).
				Erode(Shape.Diamond_5x5);

			Matrix<float> Image2 = M.
				Dilate(Shape.X_5x5).
				Erode(5);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}

		/// <summary>
		/// Highlights features in a matrix, utilizing a sequence of morphological filters.
		/// 7x7 morphological shapes are used during the transformation.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">Threshold value. If negative, the negative image
		/// will be returned using the corresponding positive threshold.</param>
		public static Matrix<float> HighlightFeatures_7x7(this Matrix<float> M, float Threshold)
		{
			Matrix<float> Image1 = M.
				Dilate(Shape.Cross_7x7).
				Erode(Shape.Diamond_7x7);

			Matrix<float> Image2 = M.
				Dilate(Shape.X_7x7).
				Erode(7);

			Image1.AbsoluteDifference(Image2);
			Image1.Threshold(Threshold);

			return Image1;
		}
	}
}
