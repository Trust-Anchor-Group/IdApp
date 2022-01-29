﻿using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;
using IdApp.Cv.Statistics;
using System;

namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<float> Convolute(this Matrix<float> M, Matrix<float> Kernel)
		{
			float Sum = Kernel.Sum();

			if (Sum != 1 && Sum != 0)
			{
				Kernel = Kernel.Copy();
				Kernel.ScalarDivision(Sum);
			}

			int KernelY, KernelHeight = Kernel.Height;
			int KernelX, KernelWidth = Kernel.Width;
			int KernelOffset = Kernel.Start;
			int KernelSkip = Kernel.Skip;
			float[] KernelData = Kernel.Data;
			int ResultWidth = M.Width - KernelWidth + 1;
			int ResultHeight = M.Height - KernelHeight + 1;
			Matrix<float> Result = new Matrix<float>(ResultWidth, ResultHeight);
			float Scalar;

			for (KernelY = 0; KernelY < KernelHeight; KernelY++, KernelOffset += KernelSkip)
			{
				for (KernelX = 0; KernelX < KernelWidth; KernelX++)
				{
					Scalar = KernelData[KernelOffset++];

					if (Scalar != 0)
					{
						Result.WeightedAddition(M.Region(KernelX, KernelY, ResultWidth, ResultHeight), Scalar);
						Sum += Scalar;
					}
				}
			}

			Result.Cap(0, 1);

			return Result;
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of convolution</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<float> Convolute(this IMatrix M, IMatrix Kernel)
		{
			if (!(M is Matrix<float> M2))
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));

			if (!(Kernel is Matrix<float> K2))
				throw new ArgumentException("Unsupported type: " + Kernel.GetType().FullName, nameof(Kernel));

			return M2.Convolute(K2);
		}

	}
}