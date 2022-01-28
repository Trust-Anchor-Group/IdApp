using IdApp.Cv.Statistics;
using System;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Erode(this Matrix<float> M)
		{
			return M.Erode(3);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> Erode(this Matrix<float> M, int NeighborhoodWidth)
		{
			if (NeighborhoodWidth <= 0 || NeighborhoodWidth >= M.Width || NeighborhoodWidth >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodWidth));

			int From = -NeighborhoodWidth / 2;
			int To = NeighborhoodWidth + From;
			int y, h = M.Height;
			int x, w = M.Width;
			int x1, y1, x2, y2;
			Matrix<float> Neighborhood = M.Region(0, 0, NeighborhoodWidth, NeighborhoodWidth);
			float[] Result = new float[w * h];
			int Index = 0;

			for (y = 0; y < h; y++)
			{
				y1 = y + From;
				if (y1 < 0)
					y1 = 0;

				y2 = y + To;
				if (y2 >= h)
					y2 = h - 1;

				Neighborhood.SetYSpan(y1 + M.Top, y2 - y1);

				for (x = 0; x < w; x++)
				{
					x1 = x + From;
					if (x1 < 0)
						x1 = 0;

					x2 = x + To;
					if (x2 >= w)
						x2 = w - 1;

					Neighborhood.SetXSpan(x1 + M.Left, x2 - x1);

					Result[Index++] = Neighborhood.Min();
				}
			}

			return new Matrix<float>(w, h, Result);
		}
	}
}
