namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects vertical edges in an image using the Sharr Vertical Edge Operator.
		/// (Second order horizontal derivative of image)
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesSharrVertical(this IMatrix M)
		{
			return M.Convolute(detectVerticalEdgesSharrKernel);
		}

		private static readonly Matrix<float> detectVerticalEdgesSharrKernel = new Matrix<float>(3, 3, new float[]
		{
			 -3,  0,  3,
			-10,  0, 10,
			 -3,  0,  3
		});
	}
}
