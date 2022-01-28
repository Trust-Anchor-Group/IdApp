using System;
using IdApp.Cv;
using IdApp.Cv.Basic;
using IdApp.Cv.Channels;
using IdApp.Cv.ColorModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class BasicTests
	{
		[TestMethod]
		public void Test_01_Red()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\Red.png");
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint> M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0xff0000ff, i);

			Matrix<byte> M3 = M2.RedChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0xff, b);
		}

		[TestMethod]
		public void Test_02_Green()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\Green.png");
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint> M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0xff00ff00, i);

			Matrix<byte> M3 = M2.GreenChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0xff, b);
		}

		[TestMethod]
		public void Test_03_Blue()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\Blue.png");
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint> M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0xffff0000, i);

			Matrix<byte> M3 = M2.BlueChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0xff, b);
		}

		[TestMethod]
		public void Test_04_Semitransparent()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\Semitransparent.png");
			Assert.AreEqual(typeof(uint), M.ElementType);
			Assert.AreEqual(100, M.Width);
			Assert.AreEqual(100, M.Height);

			Matrix<uint> M2 = M as Matrix<uint>;
			Assert.IsNotNull(M2);

			foreach (uint i in M2.Data)
				Assert.AreEqual(0x80008000, i);     // Why not 0x8000ff00 ?

			Matrix<byte> M3 = M2.AlphaChannel();

			foreach (byte b in M3.Data)
				Assert.AreEqual(0x80, b);
		}

		[TestMethod]
		public void Test_05_Grayscale()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.GrayScale();
			Bitmaps.ToImageFile(G, "Cv\\Results\\Basic\\Test_05_Grayscale.png");
		}

		[TestMethod]
		public void Test_06_ReduceColors()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Basic\\Test_06_ReduceColors.png");
		}

		[TestMethod]
		public void Test_07_ReduceColorsBW()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.GrayScale();
			G = G.ReduceColors(4);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Basic\\Test_07_ReduceColorsBW.png");
		}

		[TestMethod]
		public void Test_08_ReduceColorsBinary()
		{
			IMatrix M = Bitmaps.FromBitmapFile("Cv\\TestData\\100_pass2-uto.jpg");
			IMatrix G = M.GrayScale();
			G = G.ReduceColors(2);
			Bitmaps.ToImageFile(G, "Cv\\Results\\Basic\\Test_08_ReduceColorsBinary.png");
		}
	}
}
