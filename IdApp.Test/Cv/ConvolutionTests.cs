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
	}
}
