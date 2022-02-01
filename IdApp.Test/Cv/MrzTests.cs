using System;
using IdApp.Cv;
using IdApp.Cv.Arithmetics;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Morphological;
using IdApp.Cv.Transformations.Thresholds;
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
			G = G.GaussianBlur(3);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_03_Blur.png");
		}

		[TestMethod]
		public void Test_04_BlackHat()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_04_BlackHat.png");
		}

		[TestMethod]
		public void Test_05_Sharr()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrHorizontal();     // Detect horizontal edges=detect vertical changes
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_05_Sharr.png");
		}

		[TestMethod]
		public void Test_06_Abs()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_06_Abs.png");
		}

		[TestMethod]
		public void Test_07_Contrast()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_07_Contrast.png");
		}

		[TestMethod]
		public void Test_08_Close()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_08_Close.png");
		}

		[TestMethod]
		public void Test_09_OtsuThreshold()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			float Otsu = G.OtsuThreshold(256, 0, 1);
			Console.Out.WriteLine("Otsu threshold: " + Otsu);
			G.Threshold(Otsu);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_09_OtsuThreshold.png");
		}

		[TestMethod]
		public void Test_10_Close()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			G.Threshold(G.OtsuThreshold(256, 0, 1));
			G = G.Close(5, 21);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_10_Close.png");
		}

		[TestMethod]
		public void Test_11_Erode1()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			G.Threshold(G.OtsuThreshold(256, 0, 1));
			G = G.Close(5, 21);
			G = G.Erode();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_11_Erode1.png");
		}

		[TestMethod]
		public void Test_12_Erode2()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			G.Threshold(G.OtsuThreshold(256, 0, 1));
			G = G.Close(5, 21);
			G = G.Erode();
			G = G.Erode();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_12_Erode2.png");
		}

		[TestMethod]
		public void Test_13_Erode3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			G.Threshold(G.OtsuThreshold(256, 0, 1));
			G = G.Close(5, 21);
			G = G.Erode();
			G = G.Erode();
			G = G.Erode();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_13_Erode3.png");
		}

		[TestMethod]
		public void Test_14_Erode4()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\mrz_original.jpg", 600, 600);
			Matrix<float> G = (Matrix<float>)M.GrayScale();
			G = G.GaussianBlur(3);
			G = G.BlackHat(13, 5);
			G = G.DetectEdgesSharrVertical();     // Detect vertical edges=detect horizontal changes
			G.Abs();
			G.Contrast();
			G = G.Close(13, 5);
			G.Threshold(G.OtsuThreshold(256, 0, 1));
			G = G.Close(5, 21);
			G = G.Erode();
			G = G.Erode();
			G = G.Erode();
			G = G.Erode();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Mrz\\Test_14_Erode4.png");
		}
	}
}
