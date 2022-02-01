using IdApp.Cv.Statistics;

namespace IdApp.Cv.Transformations.Thresholds
{
	/// <summary>
	/// Static class for Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <param name="Min">Smallest value to consider (default 0)</param>
		/// <param name="Max">Largest value to consider (default 1)</param>
		/// <returns>Threshold level.</returns>
		public static float OtsuThreshold(this Matrix<float> M, int Buckets, float Min, float Max)
		{
			int[] Histogram = M.Histogram(Buckets, Min, Max);
			int total = M.Width * M.Height;
			int i, c = Buckets;

			long sumB = 0;
			float wB = 0;
			float wF, mF, val;
			float maximum = 0.0f;
			float level = 0;
			long sum1 = 0;

			for (i = 1; i < c; i++)
				sum1 += i * Histogram[i];

			for (i = 0; i < c; i++)
			{
				wF = total - wB;
				if (wB > 0 && wF > 0)
				{
					mF = (sum1 - sumB) / wF;
					val = wB * wF * ((sumB / wB) - mF) * ((sumB / wB) - mF);
					if (val >= maximum)
					{
						level = i;
						maximum = val;
					}
				}

				wB = wB + Histogram[i];
				sumB = sumB + i * Histogram[i];
			}

			return level * (Max - Min) / Buckets + Min;
		}

	}
}
