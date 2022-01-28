namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Computes the range (minimum and maximum values) in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Smallest value found.</param>
		/// <param name="Max">Largest value found.</param>
		public static void Range(this Matrix<float> M, out float Min, out float Max)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;

			Min = Max = Data[Index];

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index++];
					if (v < Min)
						Min = v;
					else if (v > Max)
						Max = v;
				}
			}
		}
	}
}
