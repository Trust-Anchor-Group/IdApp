using System;
using Waher.Content;
using Waher.Security;

namespace IdApp.DeviceSpecific.Nfc.Extensions
{
	/// <summary>
	/// Contains parsed information from a machine-readable document information string.
	/// 
	/// Reference:
	/// https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf
	/// </summary>
	public class DocumentInformation
	{
		/// <summary>
		/// Document Type
		/// </summary>
		public string DocumentType;

		/// <summary>
		/// 3-letter country code.
		/// </summary>
		public string Country;

		/// <summary>
		/// Last names
		/// </summary>
		public string[] LastNames;

		/// <summary>
		/// First names
		/// </summary>
		public string[] FirstNames;

		/// <summary>
		/// Gender
		/// </summary>
		public string Gender;

		/// <summary>
		/// Document number
		/// </summary>
		public string DocumentNumber;

		/// <summary>
		/// Date-of-birth (YYMMDD)
		/// </summary>
		public string DateOfBirth;

		/// <summary>
		/// Expiry Dte (YYMMDD)
		/// </summary>
		public string ExpiryDate;

		/// <summary>
		/// MRZ-information for use with Basic Access Control (BAC).
		/// </summary>
		public string MRZ_Information;

		/// <summary>
		/// Seed for computing cryptographic keys (§D.2)
		/// </summary>
		public byte[] KSeed
		{
			get
			{
				if (this.kseed is null)
				{
					byte[] Data = CommonTypes.ISO_8859_1.GetBytes(this.MRZ_Information);
					byte[] H = Hashes.ComputeSHA1Hash(Data);
					Array.Resize<byte>(ref H, 16);
					this.kseed = H;
				}

				return this.kseed;
			}
		}

		private byte[] kseed = null;
		private byte[] kenc = null;
		private byte[] kmac = null;

		/// <summary>
		/// 3DES Enceryption Key (§D.1)
		/// </summary>
		public byte[] KEnc
		{
			get
			{
				if (this.kenc is null)
					this.kenc = this.CalcKey(1);

				return this.kenc;
			}
		}

		/// <summary>
		/// 3DES MAC Key (§D.1)
		/// </summary>
		public byte[] KMac
		{
			get
			{
				if (this.kmac is null)
					this.kmac = this.CalcKey(2);

				return this.kmac;
			}
		}

		private byte[] CalcKey(int Counter)
		{
			byte[] D = new byte[20];
			Array.Copy(this.KSeed, 0, D, 0, 16);
			int i;

			for (i = 19; i >= 16; i--)
			{
				D[i] = (byte)(Counter);
				Counter >>= 8;
			}

			byte[] H = Hashes.ComputeSHA1Hash(D);
			Array.Resize<byte>(ref H, 16);
			OddParity(H);

			return H;
		}

		private static void OddParity(byte[] H)
		{
			int i, j, c = H.Length;
			byte b;

			for (i = 0; i < c; i++)
			{
				b = H[i];
				j = 0;

				while (b != 0)
				{
					j += (b & 1);
					b >>= 1;
				}

				if ((j & 1) == 0)
					H[i] ^= 1;
			}
		}

	}
}
