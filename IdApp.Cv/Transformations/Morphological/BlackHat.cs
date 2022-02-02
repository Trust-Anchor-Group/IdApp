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
			return M.BlackHat(NeighborhoodWidth, NeighborhoodWidth);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<float> BlackHat(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			Matrix<float> Result = M.Close(NeighborhoodWidth, NeighborhoodHeight);
			Result.AbsoluteDifference(M);
			return Result;
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<int> BlackHat(this Matrix<int> M)
		{
			return M.BlackHat(3);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<int> BlackHat(this Matrix<int> M, int NeighborhoodWidth)
		{
			return M.BlackHat(NeighborhoodWidth, NeighborhoodWidth);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<int> BlackHat(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			Matrix<int> Result = M.Close(NeighborhoodWidth, NeighborhoodHeight);
			Result.AbsoluteDifference(M);
			return Result;
		}
	}
}
