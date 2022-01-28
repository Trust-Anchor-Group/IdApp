namespace IdApp.Cv.Transformations
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Creates a black and white image based on the threshold levels provided in
		/// <paramref name="Min"/> and <paramref name="Max"/>.
		/// If Min &lt; Max, values between Min and Max become 1, and values outside of the
		/// range become 0. If Min &gt; Max, values between Max and Min become 0 and values
		/// outside of the range become 1.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Threshold(this Matrix<float> M, float Min, float Max)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;

			if (Min <= Max)
			{
				for (y = 0; y < h; y++, Index += Skip)
				{
					for (x = 0; x < w; x++)
					{
						v = Data[Index];
						Data[Index++] = v >= Min && v <= Max ? 1 : 0;
					}
				}
			}
			else
			{
				for (y = 0; y < h; y++, Index += Skip)
				{
					for (x = 0; x < w; x++)
					{
						v = Data[Index];
						Data[Index++] = v >= Min || v <= Max ? 1 : 0;
					}
				}
			}
		}
	}
}
