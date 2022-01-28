using System;

namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a scalar multiplication on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void ScalarDivision(this Matrix<float> M, float Scalar)
		{
			if (Scalar == 0)
				throw new ArgumentException("Division by zero.", nameof(Scalar));

			M.ScalarMultiplication(1.0f / Scalar);
		}
	}
}
