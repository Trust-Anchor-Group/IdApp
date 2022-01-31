namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects horizontal edges in an image using the Sharr Horizontal Edge Operator.
		/// (Second order vertical derivative of image)
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesSharrHorizontal(this IMatrix M)
		{
			return M.Convolute(detectHorizontalEdgesSharrKernel);
		}

		private static readonly Matrix<float> detectHorizontalEdgesSharrKernel = new Matrix<float>(3, 3, new float[]
		{
			-3, -10, -3,
			 0,   0,  0,
			 3,  10,  3
		});
	}
}
