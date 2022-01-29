namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects horizontal edges in an image using the Sobel Horizontal Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesSobelHorizontal(this IMatrix M)
		{
			return M.Convolute(detectHorizontalEdgesSobelKernel);
		}

		private static readonly Matrix<float> detectHorizontalEdgesSobelKernel = new Matrix<float>(3, 3, new float[]
		{
			-1, -2, -1,
			 0,  0,  0,
			 1,  2,  1
		});
	}
}
