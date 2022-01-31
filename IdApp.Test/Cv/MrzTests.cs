using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Morphological;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class MrzTests
	{
		// See also: https://www.pyimagesearch.com/2015/11/30/detecting-machine-readable-zones-in-passport-images/

		[TestMethod]
		public void Test_01_Resize()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Bitmaps.ToImageFile(M, "Cv\\Results\\Mrz\\Test_01_Resize.png");
		}

		[TestMethod]
		public void Test_02_Gray()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_02_Gray.png");
		}

		[TestMethod]
		public void Test_03_Blur()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3, 1);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_03_Blur.png");
		}

		[TestMethod]
		public void Test_04_BlackHat()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3, 1);
			G = G.BlackHat(3);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_04_BlackHat.png");
		}

		[TestMethod]
		public void Test_05_Sharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3, 1);
			G = G.BlackHat(3);
			G = G.DetectEdgesSharrVertical();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_05_Sharr.png");
		}
	}
}
