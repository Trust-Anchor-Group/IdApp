using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Morphological;
using IdApp.Cv.Transformations.Thresholds;
using System;

namespace IdApp.Cv.Objects
{
	/// <summary>
	/// Static class for Object Operations, implemented as extensions.
	/// </summary>
	public static partial class Utilities
	{
		/// <summary>
		/// Extracts the MRZ region of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>MRZ region if found, null if not found.</returns>
		public static Matrix<float> ExtractMrzRegion(this Matrix<float> M)
		{
			Matrix<float> G = M.GaussianBlur(3);
			Matrix<float> ForOcr = G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());
			G = G.Close((5 * M.Width + 208) / 415, (33 * M.Height + 300) / 600);

			Matrix<uint> Borders = new Matrix<uint>(G.Width, G.Height);
			ObjectMap ObjectMap = G.ObjectMap(0.5f);
			ObjectInformation[] Objects = ObjectMap.Objects;

			Array.Sort(Objects, (o1, o2) => o2.NrPixels - o1.NrPixels);
			Borders.Fill(0xff000000);

			foreach (ObjectInformation Object in Objects)
			{
				float Aspect = ((float)Object.Width) / Object.Height;
				if (Aspect < 5)
					continue;

				float RelativeWidth = ((float)Object.Width) / G.Width;
				if (RelativeWidth < 0.75f)
					continue;

				Point[] Reduced = Object.Contour.Reduce(10);
				if (Reduced.Length != 4)
					continue;

				Matrix<float> SubRegion = ForOcr.Region((ForOcr.Width - G.Width) / 2, (ForOcr.Height - G.Height) / 2, G.Width, G.Height);
				return ObjectMap.Extract<float>(Object.Nr, SubRegion);
			}

			return null;
		}

		/// <summary>
		/// Extracts the MRZ region of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>MRZ region if found, null if not found.</returns>
		public static Matrix<int> ExtractMrzRegion(this Matrix<int> M)
		{
			Matrix<int> G = M.GaussianBlur(3);
			Matrix<int> ForOcr = G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());
			G = G.Close((5 * M.Width + 208) / 415, (33 * M.Height + 300) / 600);

			Matrix<uint> Borders = new Matrix<uint>(G.Width, G.Height);
			ObjectMap ObjectMap = G.ObjectMap(0x00800000);
			ObjectInformation[] Objects = ObjectMap.Objects;

			Array.Sort(Objects, (o1, o2) => o2.NrPixels - o1.NrPixels);
			Borders.Fill(0xff000000);

			foreach (ObjectInformation Object in Objects)
			{
				float Aspect = ((float)Object.Width) / Object.Height;
				if (Aspect < 5)
					continue;

				float RelativeWidth = ((float)Object.Width) / G.Width;
				if (RelativeWidth < 0.75f)
					continue;

				Point[] Reduced = Object.Contour.Reduce(10);
				if (Reduced.Length != 4)
					continue;

				Matrix<int> SubRegion = ForOcr.Region((ForOcr.Width - G.Width) / 2, (ForOcr.Height - G.Height) / 2, G.Width, G.Height);
				return ObjectMap.Extract<int>(Object.Nr, SubRegion);
			}

			return null;
		}
	}
}
