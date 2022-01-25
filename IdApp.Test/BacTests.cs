using IdApp.DeviceSpecific.Nfc.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdApp.Test
{
	[TestClass]
	public class BacTests
	{
		// Testing parsing of Machine-Readable string on identity documents, in accordance with ICAO Doc 9303
		// Reference tests: §D.2, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

		[TestMethod]
		public void Test_01_TD2_9charsplus()
		{
			string Mrz = "I<UTOSTEVENSON<<PETER<JOHN<<<<<<<<<<\nD23145890<UTO3407127M95071227349<<<8";
			Assert.IsTrue(BasicAccessControl.ParseMrz(Mrz, out DocumentInformation Info));
			Assert.AreEqual("I", Info.DocumentType);
			Assert.AreEqual("UTO", Info.Country);
			Assert.AreEqual(1, Info.LastNames.Length);
			Assert.AreEqual("STEVENSON", Info.LastNames[0]);
			Assert.AreEqual(2, Info.FirstNames.Length);
			Assert.AreEqual("PETER", Info.FirstNames[0]);
			Assert.AreEqual("JOHN", Info.FirstNames[1]);
			Assert.AreEqual("M", Info.Gender);
			Assert.AreEqual("D23145890734", Info.DocumentNumber);
			Assert.AreEqual("340712", Info.DateOfBirth);
			Assert.AreEqual("950712", Info.ExpiryDate);
			Assert.AreEqual("D23145890734934071279507122", Info.MRZ_Information);
		}

		[TestMethod]
		public void Test_02_TD2_9chars()
		{
			string Mrz = "I<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<\nL898902C<3UTO6908061F9406236<<<<<<<8";
			Assert.IsTrue(BasicAccessControl.ParseMrz(Mrz, out DocumentInformation Info));
			Assert.AreEqual("I", Info.DocumentType);
			Assert.AreEqual("UTO", Info.Country);
			Assert.AreEqual(1, Info.LastNames.Length);
			Assert.AreEqual("ERIKSSON", Info.LastNames[0]);
			Assert.AreEqual(2, Info.FirstNames.Length);
			Assert.AreEqual("ANNA", Info.FirstNames[0]);
			Assert.AreEqual("MARIA", Info.FirstNames[1]);
			Assert.AreEqual("F", Info.Gender);
			Assert.AreEqual("L898902C<", Info.DocumentNumber);
			Assert.AreEqual("690806", Info.DateOfBirth);
			Assert.AreEqual("940623", Info.ExpiryDate);
			Assert.AreEqual("L898902C<369080619406236", Info.MRZ_Information);
		}

		[TestMethod]
		public void Test_03_TD1_9charsplus()
		{
			string Mrz = "I<UTOD23145890<7349<<<<<<<<<<<\n3407127M9507122UTO<<<<<<<<<<<2\nSTEVENSON<<PETER<JOHN<<<<<<<<<";
			Assert.IsTrue(BasicAccessControl.ParseMrz(Mrz, out DocumentInformation Info));
			Assert.AreEqual("I", Info.DocumentType);
			Assert.AreEqual("UTO", Info.Country);
			Assert.AreEqual(1, Info.LastNames.Length);
			Assert.AreEqual("STEVENSON", Info.LastNames[0]);
			Assert.AreEqual(2, Info.FirstNames.Length);
			Assert.AreEqual("PETER", Info.FirstNames[0]);
			Assert.AreEqual("JOHN", Info.FirstNames[1]);
			Assert.AreEqual("M", Info.Gender);
			Assert.AreEqual("D23145890734", Info.DocumentNumber);
			Assert.AreEqual("340712", Info.DateOfBirth);
			Assert.AreEqual("950712", Info.ExpiryDate);
			Assert.AreEqual("D23145890734934071279507122", Info.MRZ_Information);
		}

		[TestMethod]
		public void Test_04_TD1_9chars()
		{
			string Mrz = "I<UTOL898902C<3<<<<<<<<<<<<<<<\n6908061F9406236UTO<<<<<<<<<<<1\nERIKSSON<<ANNA<MARIA<<<<<<<<<<";
			Assert.IsTrue(BasicAccessControl.ParseMrz(Mrz, out DocumentInformation Info));
			Assert.AreEqual("I", Info.DocumentType);
			Assert.AreEqual("UTO", Info.Country);
			Assert.AreEqual(1, Info.LastNames.Length);
			Assert.AreEqual("ERIKSSON", Info.LastNames[0]);
			Assert.AreEqual(2, Info.FirstNames.Length);
			Assert.AreEqual("ANNA", Info.FirstNames[0]);
			Assert.AreEqual("MARIA", Info.FirstNames[1]);
			Assert.AreEqual("F", Info.Gender);
			Assert.AreEqual("L898902C<", Info.DocumentNumber);
			Assert.AreEqual("690806", Info.DateOfBirth);
			Assert.AreEqual("940623", Info.ExpiryDate);
			Assert.AreEqual("L898902C<369080619406236", Info.MRZ_Information);
		}
	}
}
