namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a scalar linear transform (x*Scale+Offset) on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scale">Scale factor to apply on each element.</param>
		/// <param name="Offset">Term to add after multiplication.</param>
		public static void ScalarLinearTransform(this Matrix<float> M, float Scale, float Offset)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index];
					Data[Index++] = v * Scale + Offset;
				}
			}
		}

		/// <summary>
		/// Performs a scalar linear transform ((x+PreOffset)*Scale+PostOffset) on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="PreOffset">Term to add before multiplication.</param>
		/// <param name="Scale">Scale factor to apply on each element.</param>
		/// <param name="PostOffset">Term to add after multiplication.</param>
		public static void ScalarLinearTransform(this Matrix<float> M, float PreOffset, float Scale, float PostOffset)
		{
			M.ScalarLinearTransform(Scale, PreOffset * Scale + PostOffset);
		}
	}
}
