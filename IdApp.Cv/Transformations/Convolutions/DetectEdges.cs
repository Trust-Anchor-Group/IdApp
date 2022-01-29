namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects edges in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where edges are highlighted.</returns>
		public static Matrix<float> DetectEdges(this IMatrix M)
		{
			return M.Convolute(detectEdgesKernel);
		}

		private static readonly Matrix<float> detectEdgesKernel = new Matrix<float>(3, 3, new float[]
		{
			-1, -1, -1,
			-1,  8, -1,
			-1, -1, -1
		});
	}
}
