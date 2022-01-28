namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Closes an image by first dilating it, and then eroding it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Close(this Matrix<float> M)
		{
			return M.Close(3);
		}

		/// <summary>
		/// Closes an image by first dilating it, and then eroding it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> Close(this Matrix<float> M, int NeighborhoodWidth)
		{
			return M.
				Dilate(NeighborhoodWidth).
				Erode(NeighborhoodWidth);
		}
	}
}
