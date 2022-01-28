﻿namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes the average value of all values in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static float Average(this Matrix<float> M)
		{
			return M.Sum() / (M.Width * M.Height);
		}
	}
}
