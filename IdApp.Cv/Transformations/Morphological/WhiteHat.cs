using IdApp.Cv.Arithmetics;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> WhiteHat(this Matrix<float> M)
		{
			return M.WhiteHat(3);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> WhiteHat(this Matrix<float> M, int NeighborhoodWidth)
		{
			Matrix<float> Result = M.Open(NeighborhoodWidth);
			Result.AbsoluteDifference(M);
			return Result;
		}
	}
}
