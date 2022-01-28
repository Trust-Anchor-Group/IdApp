namespace IdApp.Cv.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Sharpens an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Sharpened image.</returns>
		public static IMatrix Sharpen(this IMatrix M)
		{
			return M.Convolute(sharpenKernel);
		}

		private static readonly Matrix<float> sharpenKernel = new Matrix<float>(3, 3, new float[]
		{
			 0, -1,  0,
			-1,  5, -1,
			 0, -1,  0
		});
	}
}
