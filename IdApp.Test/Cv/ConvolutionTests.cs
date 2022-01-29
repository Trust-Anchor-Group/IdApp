using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations.Convolutions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class ConvolutionTests
	{
		[TestMethod]
		public void Test_01_Sharpen()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Sharpen();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_01_Sharpen.png");
		}

		[TestMethod]
		public void Test_02_Blur_3x3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Blur();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_02_Blur.png");
		}

		[TestMethod]
		public void Test_03_Blur_5x5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Blur(5);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_03_Blur_5x5.png");
		}


		[TestMethod]
		public void Test_04_Blur_7x7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.Blur(7);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_04_Blur_7x7.png");
		}

		/* Script to calculate Gaussian Blur convolution matrix:
		 * 
		 * GaussianBlurMatrix([NrRows],[NrColumns],[sigma]):=
		 * (
		 * 	NrRows mod 2 = 0 ? error("Odd number of rows expected.");
		 * 	NrColumns mod 2 = 0 ? error("Odd number of columns expected.");
		 * 
		 * 	x:=Columns((-(NrColumns-1)/2)..((NrColumns-1)/2));
		 * 	y:=Rows((-(NrRows-1)/2)..((NrRows-1)/2));
		 * 	M:=exp(-(x.^2+y.^2)/(2*sigma^2));
		 * 	M:=M./sum(sum(M))
		 * )
		 * 
		 * To plot matrix:
		 * 
		 * M:=GaussianBlurMatrix(7,7,1.5);
		 * VerticalBars3D(Columns(-3..3),M,Rows(-3..3))
		 */

		[TestMethod]
		public void Test_05_GaussianBlur_3x3()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(3, 1);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_05_GaussianBlur_3x3.png");
		}

		[TestMethod]
		public void Test_06_GaussianBlur_5x5()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(5, 1.25f);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_06_GaussianBlur_5x5.png");
		}

		[TestMethod]
		public void Test_07_GaussianBlur_7x7()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			G = G.GaussianBlur(7, 1.5f);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Convolutions\\Test_07_GaussianBlur_7x7.png");
		}
	}
}
