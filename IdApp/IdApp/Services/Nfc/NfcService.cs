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
							byte[] ChallengeResponse = DocInfo.CalcResponse(Challenge);
							byte[] Response = await Iso14443_4.SendResponse(ChallengeResponse);

						}
					}
				}
			}
		}
	}
}
