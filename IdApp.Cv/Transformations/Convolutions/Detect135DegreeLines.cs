namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects 135 degree lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where 135 degree lines are highlighted.</returns>
		public static Matrix<float> Detect135DegreeLines(this IMatrix M)
		{
			return M.Convolute(detect135DegreeLinesKernel);
		}

		private static readonly Matrix<float> detect135DegreeLinesKernel = new Matrix<float>(3, 3, new float[]
		{
			 2, -1, -1,
			-1,  2, -1,
			-1, -1,  2
		});
	}
}
