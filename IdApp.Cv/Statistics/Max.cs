namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes the largest value in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static float Max(this Matrix<float> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;
			float Result = Data[Index];

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index++];
					if (v > Result)
						Result = v;
				}
			}

			return Result;
		}
	}
}
