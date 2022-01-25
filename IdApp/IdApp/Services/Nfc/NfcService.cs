using IdApp.DeviceSpecific.Nfc;
using IdApp.DeviceSpecific.Nfc.Extensions;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Security;

namespace IdApp.Services.Nfc
{
	/// <summary>
	/// Near-Field Communication (NFC) Service.
	/// </summary>
	[Singleton]
	public class NfcService : ServiceReferences, INfcService
	{
		/// <summary>
		/// Near-Field Communication (NFC) Service.
		/// </summary>
		public NfcService()
			: base()
		{
		}

		/// <summary>
		/// Method called when a new NFC Tag has been detected.
		/// </summary>
		/// <param name="Tag">NFC Tag</param>
		public async Task TagDetected(INfcTag Tag)
		{
			string TagId = Hashes.BinaryToString(Tag.ID).ToUpper();
			NfcTagReference TagReference = await NfcTagReference.FindByTagId(TagId);

			foreach (INfcInterface Interface in Tag.Interfaces)
			{
				if (Interface is IIsoDepInterface Iso14443_4)
				{
					string Mrz = await RuntimeSettings.GetAsync("NFC.LastMrz", string.Empty);

					if (!string.IsNullOrEmpty(Mrz) &&
						BasicAccessControl.ParseMrz(Mrz, out DocumentInformation DocInfo))
					{
						// §4.3, §D.3, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

						byte[] Challenge = await Iso14443_4.RequestChallenge();
						if (!(Challenge is null))
						{
							byte[] Rnd1 = new byte[8];
							byte[] Rnd2 = new byte[16];

							using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
							{
								Rnd.GetBytes(Rnd1);
								Rnd.GetBytes(Rnd2);
							}

							byte[] S = new byte[8 + 8 + 16];
							byte[] EIFD;
							byte[] MIFD;

							Array.Copy(Rnd1, 0, S, 0, 8);
							Array.Copy(Challenge, 0, S, 8, 8);
							Array.Copy(Rnd2, 0, S, 16, 16);

							using (TripleDES Cipher = TripleDES.Create())
							{
								using (ICryptoTransform Encryptor = Cipher.CreateEncryptor(DocInfo.KEnc, null))
								{
									EIFD = Encryptor.TransformFinalBlock(S, 0, 32);
								}
							}


						}
					}
				}
			}
		}
	}
}
