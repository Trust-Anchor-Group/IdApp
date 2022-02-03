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
using System;
using System.IO;

namespace IdApp.Test.Cv
{
	[TestClass]
	public class MrzTests
	{
		public void ExtractMrz(int TestNr, string FileName)
		{
			IMatrix Bmp = Bitmaps.FromBitmapFile("Cv\\TestData\\" + FileName, 600, 600);
			Matrix<float> Grayscale = (Matrix<float>)Bmp.GrayScale();
			Matrix<float> Mrz = Grayscale.ExtractMrzRegion();
			Assert.IsNotNull(Mrz);
			Bitmaps.ToImageFile(Mrz, "Cv\\Results\\Mrz\\" + TestNr.ToString("D2") + "." + Path.ChangeExtension(FileName, "png"));
		}

		[TestMethod]
		public void Test_01()
		{
			this.ExtractMrz(1, "100_visa-usa.jpg");
		}

		[TestMethod]
		public void Test_02()
		{
			this.ExtractMrz(2, "0_id-esp.png");
		}

		[TestMethod]
		public void Test_03()
		{
			this.ExtractMrz(3, "0_pass-lva.jpg");
		}

		[TestMethod]
		public void Test_04()
		{
			this.ExtractMrz(4, "24_pass-egy.jpg");
		}

		[TestMethod]
		public void Test_05()
		{
			this.ExtractMrz(5, "25_pass-uto.jpg");
		}

		[TestMethod]
		public void Test_06()
		{
			this.ExtractMrz(6, "27_pass-gbr.jpg");
		}

		[TestMethod]
		public void Test_07()
		{
			this.ExtractMrz(7, "33_id-usa.jpg");
		}

		[TestMethod]
		public void Test_08()
		{
			this.ExtractMrz(8, "43_pass-fra.jpg");
		}

		[TestMethod]
		public void Test_09()
		{
			this.ExtractMrz(9, "43_pass-twn.jpg");
		}

		[TestMethod]
		public void Test_10()
		{
			this.ExtractMrz(10, "53_id-d.jpg");
		}

		[TestMethod]
		public void Test_11()
		{
			this.ExtractMrz(11, "62_pass-aus.png");
		}

		[TestMethod]
		public void Test_12()
		{
			this.ExtractMrz(12, "62_pass-pol.png");
		}

		[TestMethod]
		public void Test_13()
		{
			this.ExtractMrz(13, "77_card-cmw.png");
		}

		[TestMethod]
		public void Test_14()
		{
			this.ExtractMrz(14, "79_pass-can.jpg");
		}

		[TestMethod]
		public void Test_15()
		{
			this.ExtractMrz(15, "79_pass-hun.png");
		}

		[TestMethod]
		public void Test_16()
		{
			this.ExtractMrz(16, "98_pass-nld.jpg");
		}

		[TestMethod]
		public void Test_17()
		{
			this.ExtractMrz(17, "100_id-che.jpg");
		}

		[TestMethod]
		public void Test_18()
		{
			this.ExtractMrz(18, "100_id-mac.jpg");
		}

		[TestMethod]
		public void Test_19()
		{
			this.ExtractMrz(19, "100_id-rou.jpg");
		}

		[TestMethod]
		public void Test_20()
		{
			this.ExtractMrz(20, "100_id-si.jpg");
		}

		[TestMethod]
		public void Test_21()
		{
			this.ExtractMrz(21, "100_id-usa.jpg");
		}

		[TestMethod]
		public void Test_22()
		{
			this.ExtractMrz(22, "100_pass2-uto.jpg");
		}

		[TestMethod]
		public void Test_23()
		{
			this.ExtractMrz(23, "100_pass-bdr.jpg");
		}

		[TestMethod]
		public void Test_24()
		{
			this.ExtractMrz(24, "100_pass-chn.jpg");
		}

		[TestMethod]
		public void Test_25()
		{
			this.ExtractMrz(25, "100_pass-cze.jpg");
		}

		[TestMethod]
		public void Test_26()
		{
			this.ExtractMrz(26, "100_pass-cze2.jpg");
		}

		[TestMethod]
		public void Test_27()
		{
			this.ExtractMrz(27, "100_pass-fin.png");
		}

		[TestMethod]
		public void Test_28()
		{
			this.ExtractMrz(28, "100_pass-hrv.jpg");
		}

		[TestMethod]
		public void Test_29()
		{
			this.ExtractMrz(29, "100_pass-isl.png");
		}

		[TestMethod]
		public void Test_30()
		{
			this.ExtractMrz(30, "100_pass-ltu.jpg");
		}

		[TestMethod]
		public void Test_31()
		{
			this.ExtractMrz(31, "100_pass-lux.jpg");
		}

		[TestMethod]
		public void Test_32()
		{
			this.ExtractMrz(32, "100_pass-polx.jpg");
		}

		[TestMethod]
		public void Test_33()
		{
			this.ExtractMrz(33, "100_pass-uto.jpg");
		}

		[TestMethod]
		public void Test_34()
		{
			this.ExtractMrz(34, "100_visa-polx.jpg");
		}

		[TestMethod]
		public void Test_35()
		{
			this.ExtractMrz(34, "mrz_original.jpg");
		}
	}
}
