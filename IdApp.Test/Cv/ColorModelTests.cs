using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class ColorModelTests
	{
		[TestMethod]
		public void Test_01_Grayscale()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.GrayScale();
			Bitmaps.ToImageFile(G, "Cv\\Results\\ColorModels\\Test_01_Grayscale.png");
		}

		[TestMethod]
		public void Test_02_ReduceColors()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Cv\\Results\\ColorModels\\Test_02_ReduceColors.png");
		}

		[TestMethod]
		public void Test_03_ReduceColorsBW()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.GrayScale();
			G = G.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Cv\\Results\\ColorModels\\Test_03_ReduceColorsBW.png");
		}

		[TestMethod]
		public void Test_04_ReduceColorsBinary()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.GrayScale();
			G = G.ReduceColors(2);
			Bitmaps.ToImageFile(G, "Cv\\Results\\ColorModels\\Test_04_ReduceColorsBinary.png");
		}
	}
}
