using System;
using System.Collections.Generic;
using IdApp.Services;
using IdApp.Services.Tag;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Test.Security
{
	[TestClass]
	public class PinStrengthTests
	{
		private const string pnr = "zxcv";
		private const string phoneNumber = "vbnm";
		private const string addressPart1 = "uiop";
		private const string addressPart2 = "lkjh";
		private const string firstName = "Erwin";
		private const string middleName1 = "Rudolf";
		private const string middleName2 = "Josef";
		private const string middleName3 = "Alexander";
		private const string lastName = "Schrödinger";

		private ITagProfile tagProfile;

		[TestInitialize]
		public void BuildTagProfile()
		{
			LegalIdentity LegalIdentity = new()
			{
				ClientKeyName = "ed448",
				ClientPubKey = Convert.FromBase64String("W/MYNCYCjHP6ac7IY5Tx7wnQveNzPW19eKbdwyziUwWAZHvdKL54o5J/sVVyU/PyTZ+YpIBKNueA"),
				ClientSignature = Convert.FromBase64String("EKAu3C4UzKwLjBNGktDO55QZC/z5bLVhyTZU0NHGBgJsb1cZbH2Ap8MzAaT646HsvWNRLbtclD0AVPsYGMmoT3cnbFwZ4U8plhdJQyBGiXlzF+VXqAjp2U6QkDHPelEb6NeSBw5FBB9QhPwMJCpNvCgA"),
				Created = new DateTime(637883908780000000),
				From = new DateTime(637883424000000000),
				Id = "2a165f5e-8da1-d018-3c04-a62ee6b7f966@legal.lab.tagroot.io",
				Provider = "legal.lab.tagroot.io",
				ServerSignature = Convert.FromBase64String("v1hgCI5NZ9ngf2Lu58ud/YpaveA5ksbcyXI18+ozsYdwKghf7Smis2htfCYHpEQhWOz2MsE+bdqALzbpr+yMlPcH8tJHL9GGnRtPFNiWHUjuVqhKgHViwTQFw5SC7G7MUGR2ULQ0MT3g48KnYaeN4jAA"),
				State = IdentityState.Approved,
				To = new DateTime(638515008000000000),
				Updated = new DateTime(637883911920000000),

				Properties = new Property[]
				{
					new Property("FIRST", firstName),
					new Property("MIDDLE", middleName1 + " "+ middleName2 + " " + middleName3),
					new Property("LAST", lastName),
					new Property("PNR", pnr),
					new Property("ADDR", addressPart1 + " " + addressPart2),
					new Property("ZIP", "220000"),
					new Property("CITY", "Minsk"),
					new Property("COUNTRY", "BY"),
					new Property("PHONE", phoneNumber),
					new Property("JID", "Anton.Sakovich@lab.tagroot.io"),
				}
			};

			TagConfiguration TagConfiguration = new()
			{
				LegalIdentity = LegalIdentity,
			};

			this.tagProfile = new TagProfile();
			this.tagProfile.FromConfiguration(TagConfiguration);
		}

		[TestMethod("A null or empty PIN")]
		public void PinIsNullOrEmptry_PinWithNotEnoughDigitsLettersSigns()
		{
			PinStrength NullPinStrength = this.tagProfile.ValidatePinStrength(null);
			PinStrength EmptyPinStrength = this.tagProfile.ValidatePinStrength(string.Empty);

			Assert.AreEqual(PinStrength.NotEnoughDigitsLettersSigns, NullPinStrength, "Null PIN strength is {0}", NullPinStrength);
			Assert.AreEqual(PinStrength.NotEnoughDigitsLettersSigns, EmptyPinStrength, "Empty PIN strength is {0}", EmptyPinStrength);
		}

		[DataTestMethod]
		[DataRow("a")]
		[DataRow("1")]
		[DataRow("-")]
		[DataRow("a1")]
		[DataRow("1a")]
		[DataRow("a-")]
		[DataRow("-a")]
		[DataRow("-1")]
		[DataRow("1-")]
		[DataRow("a1-")]
		public void PinWithNotEnoughDigitsLettersSigns(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.NotEnoughDigitsLettersSigns, PinStrength);
		}

		[DataTestMethod]
		[DataRow("qwe")]
		[DataRow("qwer")]
		[DataRow("qwert")]
		[DataRow("qwerty")]
		[DataRow("qwertyu")]
		[DataRow("qwertyui")]
		[DataRow("1we")]
		[DataRow("q1er")]
		[DataRow("qw1rt")]
		[DataRow("qwe1ty")]
		[DataRow("qwer1yu")]
		[DataRow("qwert1ui")]
		[DataRow("-we")]
		[DataRow("q-er")]
		[DataRow("qw-rt")]
		[DataRow("qwe-ty")]
		[DataRow("qwer-yu")]
		[DataRow("qwert-ui")]
		public void PinWithNotEnoughDigitsOrSigns(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.NotEnoughDigitsOrSigns, PinStrength);
		}

		[DataTestMethod]
		[DataRow("135")]
		[DataRow("1357")]
		[DataRow("13579")]
		[DataRow("135792")]
		[DataRow("1357924")]
		[DataRow("13579246")]
		[DataRow("a35")]
		[DataRow("1a57")]
		[DataRow("13a79")]
		[DataRow("135a92")]
		[DataRow("1357a24")]
		[DataRow("13579a46")]
		[DataRow("-35")]
		[DataRow("1-57")]
		[DataRow("13-79")]
		[DataRow("135-92")]
		[DataRow("1357-24")]
		[DataRow("13579-46")]
		public void PinWithNotEnoughLettersOrSigns(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.NotEnoughLettersOrSigns, PinStrength);
		}

		[DataTestMethod]
		[DataRow("!#%")]
		[DataRow("!#%&")]
		[DataRow("!#%&(")]
		[DataRow("!#%&(@")]
		[DataRow("!#%&(@$")]
		[DataRow("!#%&(@$^")]
		[DataRow("1#%")]
		[DataRow("!1%&")]
		[DataRow("!#1&(")]
		[DataRow("!#%1(@")]
		[DataRow("!#%&1@$")]
		[DataRow("!#%&(1$^")]
		[DataRow("a#%")]
		[DataRow("!a%&")]
		[DataRow("!#a&(")]
		[DataRow("!#%a(@")]
		[DataRow("!#%&a@$")]
		[DataRow("!#%&(a$^")]
		public void PinWithNotEnoughLettersOrDigits(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.NotEnoughLettersOrDigits, PinStrength);
		}

		[DataTestMethod]
		[DataRow("xxx")]
		[DataRow("1xxx")]
		[DataRow("xxx1")]
		[DataRow("10xxx")]
		[DataRow("1xxx0")]
		[DataRow("xxx10")]
		[DataRow("10axxx")]
		[DataRow("10xxxa")]
		[DataRow("1xxx0a")]
		[DataRow("xxx10a")]
		[DataRow("10abxxx")]
		[DataRow("10axxxb")]
		[DataRow("10xxxab")]
		[DataRow("1xxx0ab")]
		[DataRow("xxx10ab")]
		public void PinWithTooManyIdenticalSymbols(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.TooManyIdenticalSymbols, PinStrength);
		}

		[DataTestMethod]
		[DataRow("xyz")]
		[DataRow("1" + "xyz")]
		[DataRow("xyz" + "1")]
		[DataRow("10" + "xyz")]
		[DataRow("1"+ "xyz" + "0")]
		[DataRow("xyz" + "10")]
		[DataRow("10a" + "xyz")]
		[DataRow("10" + "xyz" + "a")]
		[DataRow("1" + "xyz" + "0a")]
		[DataRow("xyz" + "10a")]
		[DataRow("10ab" + "xyz")]
		[DataRow("10a" + "xyz" + "b")]
		[DataRow("10" + "xyz" + "ab")]
		[DataRow("1" + "xyz" + "0ab")]
		[DataRow("xyz" + "10ab")]
		public void PinWithTooManySequencedSymbols(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.TooManySequencedSymbols, PinStrength);
		}

		[DataTestMethod]
		[DataRow("12ab")]
		[DataRow("12ab3")]
		[DataRow("12ab34")]
		[DataRow("12ab34c")]
		public void PinTooShort(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.TooShort, PinStrength);
		}

		[DataTestMethod]
		[DynamicData(nameof(GetTestDataForPinContainsAddress), DynamicDataSourceType.Method)]
		public void PinContainsAddress(string Pin)
		{
			PinStrength PinStrength = this.tagProfile.ValidatePinStrength(Pin);
			Assert.AreEqual(PinStrength.ContainsAddress, PinStrength);
		}

		public static IEnumerable<object[]> GetTestDataForPinContainsAddress()
		{
			string[] TargetStrings = new string[]
			{
				"",
				"1",
				"12",
				"12a",
				"12ab",
				"12ab3",
				"12ab34",
				"12ab34c",
				"12ab34cd"
			};

			foreach (string TargetString in TargetStrings)
			{
				for (int i = 0; i <= TargetString.Length; i++)
				{
					yield return new object[] { TargetString.Substring(0, i) + addressPart1 + TargetString.Substring(i) };
					yield return new object[] { TargetString.Substring(0, i) + addressPart2 + TargetString.Substring(i) };
				}
			}
		}
	}
}
