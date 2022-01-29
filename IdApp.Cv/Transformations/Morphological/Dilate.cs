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
		/// Dilates an image by replacing a pixel with the maximum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Dilate(this Matrix<float> M)
		{
			return Dilate(M, 3);
		}
		
		/// <summary>
		/// Dilates an image by replacing a pixel with the maximum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> Dilate(this Matrix<float> M, int NeighborhoodWidth)
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

					Result[Index++] = Neighborhood.Max();
				}
			}

			return new Matrix<float>(w, h, Result);
		}

		/// <summary>
		/// Dilates an image by replacing a pixel with the maximum value of pixels in a
		/// neighborhood of the pixel determined by the <paramref name="Kernel"/>.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<float> Dilate(this Matrix<float> M, Shape Kernel)
		{
			int KernelWidth = Kernel.Width;
			int KernelHeight = Kernel.Height;

			if (KernelWidth >= M.Width)
				throw new ArgumentOutOfRangeException(nameof(Kernel), "Kernel too wide for iamge.");

			if (KernelHeight >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(Kernel), "Kernel too high for iamge.");

			int FromX = -Kernel.PixelX;
			int ToX = KernelWidth - Kernel.PixelX - 1;
			int FromY = -Kernel.PixelY;
			int ToY = KernelHeight - Kernel.PixelY - 1;
			int y, h = M.Height;
			int x, w = M.Width;
			int x1, y1, x2, y2, kx1, ky1, kx2, ky2;
			Matrix<float> Neighborhood = M.Region(0, 0, KernelWidth, KernelHeight);
			Matrix<bool> KernelNeighborhood = Kernel.ShallowCopy();
			float[] Result = new float[w * h];
			int Index = 0;

			for (y = 0; y < h; y++)
			{
				y1 = y + FromY;
				if (y1 < 0)
				{
					ky1 = -y1;
					y1 = 0;
				}
				else
					ky1 = 0;

				y2 = y + ToY;
				if (y2 >= h)
				{
					ky2 = KernelHeight - (y2 - h) - 1;
					y2 = h - 1;
				}
				else
					ky2 = KernelHeight;

				Neighborhood.SetYSpan(y1 + M.Top, y2 - y1);
				KernelNeighborhood.SetYSpan(ky1 + Kernel.Top, ky2 - ky1);

				for (x = 0; x < w; x++)
				{
					x1 = x + FromX;
					if (x1 < 0)
					{
						kx1 = -x1;
						x1 = 0;
					}
					else
						kx1 = 0;

					x2 = x + ToX;
					if (x2 >= w)
					{
						kx2 = KernelWidth - (x2 - w) - 1;
						x2 = w - 1;
					}
					else
						kx2 = KernelWidth;

					Neighborhood.SetXSpan(x1 + M.Left, x2 - x1);
					KernelNeighborhood.SetXSpan(kx1 + Kernel.Left, kx2 - kx1);

					Result[Index++] = Neighborhood.Max(KernelNeighborhood);
				}
			}

			return new Matrix<float>(w, h, Result);
		}
	}
}
