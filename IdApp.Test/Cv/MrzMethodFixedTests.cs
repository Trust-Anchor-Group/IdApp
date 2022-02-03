using System;
using IdApp.Cv;
using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Objects;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Morphological;
using IdApp.Cv.Transformations.Thresholds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class MrzMethodFixedTests
	{
		// See also: https://www.pyimagesearch.com/2015/11/30/detecting-machine-readable-zones-in-passport-images/

		[TestMethod]
		public void Test_01_Resize()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Bitmaps.ToImageFile(M, "Cv\\Results\\MrzMethodFixed\\Test_01_Resize.png");
		}

		[TestMethod]
		public void Test_02_Gray()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_02_Gray.png");
		}

		[TestMethod]
		public void Test_03_Blur()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_03_Blur.png");
		}

		[TestMethod]
		public void Test_04_BlackHat()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_04_BlackHat.png");
		}

		[TestMethod]
		public void Test_05_Sharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrHorizontal();     // Detect horizontal edges=detect vertical changes
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_05_Sharr.png");
		}

		[TestMethod]
		public void Test_06_Abs()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_06_Abs.png");
		}

		[TestMethod]
		public void Test_07_Contrast()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_07_Contrast.png");
		}

		[TestMethod]
		public void Test_08_Close()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_08_Close.png");
		}

		[TestMethod]
		public void Test_09_OtsuThreshold()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			int Otsu = G.OtsuThreshold();
			Console.Out.WriteLine("Otsu threshold: " + Otsu);
			G.Threshold(Otsu);
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_09_OtsuThreshold.png");
		}

		[TestMethod]
		public void Test_10_Close()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());
			G = G.Close((5 * M.Width + 208) / 415, (33 * M.Height + 300) / 600);
			Bitmaps.ToImageFile(G, "Cv\\Results\\MrzMethodFixed\\Test_10_Close.png");
		}

		[TestMethod]
		public void Test_11_ObjectMap()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());
			G = G.Close((5 * M.Width + 208) / 415, (33 * M.Height + 300) / 600);

			Matrix<uint> Borders = new Matrix<uint>(G.Width, G.Height);
			ObjectMap ObjectMap = G.ObjectMap(0x800000);
			ushort i;
			int c = ObjectMap.NrObjects;

			Borders.Fill(0xff000000);

			for (i = 0; i < c; i++)
			{
				ObjectInformation Info = ObjectMap[i];

				foreach (Point Point in Info.Contour)
					Borders[Point.X, Point.Y] = 0xff80ff80;
			}

			Bitmaps.ToImageFile(Borders, "Cv\\Results\\MrzMethodFixed\\Test_11_ObjectMap.png");
		}

		[TestMethod]
		public void Test_12_ReduceContours()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = (Matrix<int>)M.GrayScaleFixed();
			G = G.GaussianBlur(3);
			G = G.BlackHat((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close((13 * M.Width + 208) / 415, (5 * M.Height + 300) / 600);
			G.Threshold(G.OtsuThreshold());
			G = G.Close((5 * M.Width + 208) / 415, (33 * M.Height + 300) / 600);

			Matrix<uint> Borders = new Matrix<uint>(G.Width, G.Height);
			ObjectMap ObjectMap = G.ObjectMap(0x00080000);
			ObjectInformation[] Objects = ObjectMap.Objects;

			Array.Sort(Objects, (o1, o2) => o2.NrPixels - o1.NrPixels);
			Borders.Fill(0xff000000);

			foreach (ObjectInformation Object in Objects)
			{
				foreach (Point Point in Object.Contour)
					Borders[Point.X, Point.Y] = 0xff80ff80;

				Point[] Reduced = Object.Contour.Reduce(10);
				Point Last = Reduced[Reduced.Length - 1];

				foreach (Point Point in Reduced)
				{
					Borders.Line(Last.X, Last.Y, Point.X, Point.Y, 0xffa0a0ff);
					Last = Point;
				}
			}

			Bitmaps.ToImageFile(Borders, "Cv\\Results\\MrzMethodFixed\\Test_12_ReduceContours.png");
		}

		[TestMethod]
		public void Test_13_Candidate()
		{
			Matrix<uint> M = (Matrix<uint>)Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<int> G = M.GrayScaleFixed();
			G = G.GaussianBlur(3);
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
				IMatrix Obj = ObjectMap.Extract(Object.Nr, SubRegion);
				Bitmaps.ToImageFile(Obj, "Cv\\Results\\MrzMethodFixed\\Test_13_Candidate.png");
				break;
			}
		}

	}
}
