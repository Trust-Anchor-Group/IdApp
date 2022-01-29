namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects horizontal lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal lines are highlighted.</returns>
		public static Matrix<float> DetectHorizontalLines(this IMatrix M)
		{
			return M.Convolute(detectHorizontalLinesKernel);
		}

		private static readonly Matrix<float> detectHorizontalLinesKernel = new Matrix<float>(3, 3, new float[]
		{
			-1, -1, -1,
			 2,  2,  2,
			-1, -1, -1
		});
	}
}
