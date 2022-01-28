using System;
using IdApp.Cv;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Statistics;
using IdApp.Cv.Transformations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class StatisticsTests
	{
		[TestMethod]
		public void Test_01_Min()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			Console.Out.WriteLine("Min: " + G2.Min().ToString());
		}

		[TestMethod]
		public void Test_01_Max()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			Console.Out.WriteLine("Max: " + G2.Max().ToString());
		}

		[TestMethod]
		public void Test_03_Range()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_id-si.jpg", 600, 600);
			IMatrix G = M.GrayScale();
			Matrix<float> G2 = G as Matrix<float>;
			Assert.IsNotNull(G2);
			G2.Range(out float Min, out float Max);
			Console.Out.WriteLine("Min: " + Min.ToString());
			Console.Out.WriteLine("Max: " + Max.ToString());
		}
	}
}
