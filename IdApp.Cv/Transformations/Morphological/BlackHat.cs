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
		public static Matrix<float> BlackHat(this Matrix<float> M)
		{
			return M.BlackHat(3);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> BlackHat(this Matrix<float> M, int NeighborhoodWidth)
		{
			Matrix<float> Result = M.Close(NeighborhoodWidth);
			Result.AbsoluteDifference(M);
			return Result;
		}
	}
}
