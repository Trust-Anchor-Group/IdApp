using IdApp.DeviceSpecific.Nfc.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;
using Waher.Security;

namespace IdApp.Test
{
	[TestClass]
	public class BacTests
	{
		// Testing parsing of Machine-Readable string on identity documents, in accordance with ICAO Doc 9303
		// Reference tests: §D, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

		[TestMethod]
		public void Test_01_Parse_MRZ_TD2_9charsplus()
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

			string KSeed = Hashes.BinaryToString(Info.KSeed());
			Console.Out.WriteLine("KSeed: " + KSeed);

			string KEnc = Hashes.BinaryToString(Info.KEnc());
			Console.Out.WriteLine("KEnc: " + KEnc);

			string KMac = Hashes.BinaryToString(Info.KMac());
			Console.Out.WriteLine("KMac: " + KMac);
		}

		[TestMethod]
		public void Test_02_Parse_MRZ_TD2_9chars()
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
			Assert.AreEqual("L898902C", Info.DocumentNumber);
			Assert.AreEqual("690806", Info.DateOfBirth);
			Assert.AreEqual("940623", Info.ExpiryDate);
			Assert.AreEqual("L898902C<369080619406236", Info.MRZ_Information);

			string KSeed = Hashes.BinaryToString(Info.KSeed());
			Console.Out.WriteLine("KSeed: " + KSeed);
			Assert.AreEqual("239AB9CB282DAF66231DC5A4DF6BFBAE", KSeed.ToUpper());

			string KEnc = Hashes.BinaryToString(Info.KEnc());
			Console.Out.WriteLine("KEnc: " + KEnc);
			Assert.AreEqual("AB94FDECF2674FDFB9B391F85D7F76F2", KEnc.ToUpper());

			string KMac = Hashes.BinaryToString(Info.KMac());
			Console.Out.WriteLine("KMac: " + KMac);
			Assert.AreEqual("7962D9ECE03D1ACD4C76089DCE131543", KMac.ToUpper());
		}

		[TestMethod]
		public void Test_03_Parse_MRZ_TD1_9charsplus()
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

			string KSeed = Hashes.BinaryToString(Info.KSeed());
			Console.Out.WriteLine("KSeed: " + KSeed);

			string KEnc = Hashes.BinaryToString(Info.KEnc());
			Console.Out.WriteLine("KEnc: " + KEnc);

			string KMac = Hashes.BinaryToString(Info.KMac());
			Console.Out.WriteLine("KMac: " + KMac);
		}

		[TestMethod]
		public void Test_04_Parse_MRZ_TD1_9chars()
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
			Assert.AreEqual("L898902C", Info.DocumentNumber);
			Assert.AreEqual("690806", Info.DateOfBirth);
			Assert.AreEqual("940623", Info.ExpiryDate);
			Assert.AreEqual("L898902C<369080619406236", Info.MRZ_Information);

			string KSeed = Hashes.BinaryToString(Info.KSeed());
			Console.Out.WriteLine("KSeed: " + KSeed);
			Assert.AreEqual("239AB9CB282DAF66231DC5A4DF6BFBAE", KSeed.ToUpper());

			string KEnc = Hashes.BinaryToString(Info.KEnc());
			Console.Out.WriteLine("KEnc: " + KEnc);
			Assert.AreEqual("AB94FDECF2674FDFB9B391F85D7F76F2", KEnc.ToUpper());

			string KMac = Hashes.BinaryToString(Info.KMac());
			Console.Out.WriteLine("KMac: " + KMac);
			Assert.AreEqual("7962D9ECE03D1ACD4C76089DCE131543", KMac.ToUpper());
		}

		[TestMethod]
		public void Test_05_BAC_ChallengeResponse()
		{
			byte[] Challenge = Hashes.StringToBinary("4608F91988702212");
			byte[] Rnd1 = Hashes.StringToBinary("781723860C06C226");
			byte[] Rnd2 = Hashes.StringToBinary("0B795240CB7049B01C19B33E32804F0B");
			byte[] KEnc = Hashes.StringToBinary("AB94FDECF2674FDFB9B391F85D7F76F2");
			byte[] KMac = Hashes.StringToBinary("7962D9ECE03D1ACD4C76089DCE131543");

			byte[] Response = BasicAccessControl.CalcResponse(Challenge, Rnd1, Rnd2, KEnc, KMac);

			Assert.AreEqual("72C29C2371CC9BDB65B779B8E8D37B29ECC154AA56A8799FAE2F498F76ED92F25F1448EEA8AD90A7",
				Hashes.BinaryToString(Response).ToUpper());
		}

	}
}
