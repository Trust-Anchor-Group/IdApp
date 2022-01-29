namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Open(this Matrix<float> M)
		{
			return M.Open(3);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> Open(this Matrix<float> M, int NeighborhoodWidth)
		{
			return M.
				Erode(NeighborhoodWidth).
				Dilate(NeighborhoodWidth);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<float> Open(this Matrix<float> M, Shape Kernel)
		{
			return M.
				Erode(Kernel).
				Dilate(Kernel);
		}
	}
}
