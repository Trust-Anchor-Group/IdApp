using IdApp.Cv.Basic;
using System;

namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Blurs an image using Gaussian Blur.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="KernelWidth">Width of kernel.</param>
		/// <returns>Blurred image.</returns>
		public static Matrix<float> GaussianBlur(this IMatrix M, int KernelWidth, float Sigma)
		{
			Matrix<float> Kernel = new Matrix<float>(KernelWidth, KernelWidth);
			float d = -KernelWidth * 0.5f;
			float x, x2, y, s2;
			int i, j;

			s2 = 1.0f / (Sigma * Sigma * 2);

			for (i = 0; i < KernelWidth; i++)
			{
				x = i + d;
				x2 = x * x;

				for (j = 0; j < KernelWidth; j++)
				{
					y = j + d;

					Kernel[i, j] = (float)Math.Exp(-(x2 + y * y) * s2);
				}
			}

			return M.Convolute(Kernel);
		}
	}
}
