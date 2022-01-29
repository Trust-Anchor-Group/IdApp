using IdApp.Cv.Basic;

namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Blurs an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Blurred image.</returns>
		public static Matrix<float> Blur(this IMatrix M)
		{
			return M.Convolute(blurKernel_3x3);
		}

		private static readonly Matrix<float> blurKernel_3x3 = new Matrix<float>(3, 3, new float[]
		{
			 1,  1,  1,
			 1,  1,  1,
			 1,  1,  1
		});

		/// <summary>
		/// Blurs an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="KernelWidth">Width of kernel.</param>
		/// <returns>Blurred image.</returns>
		public static Matrix<float> Blur(this IMatrix M, int KernelWidth)
		{
			Matrix<float> Kernel = new Matrix<float>(KernelWidth, KernelWidth);
			Kernel.Fill(1);
			return M.Convolute(Kernel);
		}
	}
}
