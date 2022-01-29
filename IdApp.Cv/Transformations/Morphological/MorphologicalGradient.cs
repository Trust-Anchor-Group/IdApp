﻿using IdApp.Cv.Arithmetics;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M)
		{
			return M.MorphologicalGradient(3);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M, int NeighborhoodWidth)
		{
			Matrix<float> Result = M.Dilate(NeighborhoodWidth);
			Result.AbsoluteDifference(M.Erode(NeighborhoodWidth));
			return Result;
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M, Shape Kernel)
		{
			Matrix<float> Result = M.Dilate(Kernel);
			Result.AbsoluteDifference(M.Erode(Kernel));
			return Result;
		}
	}
}