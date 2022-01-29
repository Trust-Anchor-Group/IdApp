using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Morphological;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class MorphologicalTests
	{
		[TestMethod]
		public void Test_01_Erode3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Erode(3);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_01_Erode3.png");
		}

		[TestMethod]
		public void Test_02_Erode5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Erode(5);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_02_Erode5.png");
		}

		[TestMethod]
		public void Test_03_Erode7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Erode(7);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_03_Erode7.png");
		}

		[TestMethod]
		public void Test_04_Dilate3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Dilate(3);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_04_Dilate3.png");
		}

		[TestMethod]
		public void Test_05_Dilate5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Dilate(5);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_05_Dilate5.png");
		}

		[TestMethod]
		public void Test_06_Dilate7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Dilate(7);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_06_Dilate7.png");
		}

		[TestMethod]
		public void Test_07_Open3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Open(3);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_07_Open3.png");
		}

		[TestMethod]
		public void Test_08_Open5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Open(5);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_08_Open5.png");
		}

		[TestMethod]
		public void Test_09_Open7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Open(7);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_09_Open7.png");
		}

		[TestMethod]
		public void Test_10_Close3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Close(3);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_10_Close3.png");
		}

		[TestMethod]
		public void Test_11_Close5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Close(5);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_11_Close5.png");
		}

		[TestMethod]
		public void Test_12_Close7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.Close(7);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_12_Close7.png");
		}

		[TestMethod]
		public void Test_13_HighlightFeatures3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.HighlightFeatures_3x3(0.1f);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_13_HighlightFeatures3.png");
		}

		[TestMethod]
		public void Test_14_HighlightFeatures5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.HighlightFeatures_5x5(0.1f);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_14_HighlightFeatures5.png");
		}

		[TestMethod]
		public void Test_15_HighlightFeatures7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass-lux.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.AdaptiveThreshold(0.2f, 21);
			G2 = G2.HighlightFeatures_7x7(0.1f);
			Bitmaps.ToImageFile(G2, "Cv\\Results\\Morphological\\Test_15_HighlightFeatures7.png");
		}
	}
}
