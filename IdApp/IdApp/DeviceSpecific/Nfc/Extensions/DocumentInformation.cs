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
		/// Seed for computing cryptographic keys
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



	}
}
