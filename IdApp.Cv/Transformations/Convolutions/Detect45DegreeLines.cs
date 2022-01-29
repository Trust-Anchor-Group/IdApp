namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects 45 degree lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where 45 degree lines are highlighted.</returns>
		public static Matrix<float> Detect45DegreeLines(this IMatrix M)
		{
			return M.Convolute(detect45DegreeLinesKernel);
		}

		private static readonly Matrix<float> detect45DegreeLinesKernel = new Matrix<float>(3, 3, new float[]
		{
			-1, -1,  2,
			-1,  2, -1,
			 2, -1, -1
		});
	}
}
