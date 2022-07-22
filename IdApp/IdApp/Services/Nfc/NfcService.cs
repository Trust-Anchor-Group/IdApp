using IdApp.Nfc;
using IdApp.Nfc.Extensions;
using IdApp.Nfc.Records;
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
				if (Interface is IIsoDepInterface IsoDep)
				{
					// ISO 14443-4

					string Mrz = await RuntimeSettings.GetAsync("NFC.LastMrz", string.Empty);

					if (!string.IsNullOrEmpty(Mrz) &&
						BasicAccessControl.ParseMrz(Mrz, out DocumentInformation DocInfo))
					{
						// §4.3, §D.3, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

						byte[] Challenge = await IsoDep.GetChallenge();
						if (!(Challenge is null))
						{
							byte[] ChallengeResponse = DocInfo.CalcChallengeResponse(Challenge);
							byte[] Response = await IsoDep.ExternalAuthenticate(ChallengeResponse);

							// TODO
						}
					}
				}
				else if (Interface is INdefInterface Ndef)
				{
					bool CanMakeReadOnly = await Ndef.CanMakeReadOnly();
					bool IsWritable = await Ndef.IsWritable();
					INdefRecord[] Records = await Ndef.GetMessage();

					// TODO
				}
				//else if (Interface is INfcAInterface NfcA)
				//{
				//	byte[] Atqa = await NfcA.GetAtqa();
				//	short Sqk = await NfcA.GetSqk();
				//
				//	// TODO
				//}
				//else if (Interface is INfcBInterface NfcB)
				//{
				//	byte[] ApplicationData = await NfcB.GetApplicationData();
				//	byte[] ProtocolInfo = await NfcB.GetProtocolInfo();
				//
				//	// TODO
				//}
				//else if (Interface is INfcFInterface NfcF)
				//{
				//	byte[] Manufacturer = await NfcF.GetManufacturer();
				//	byte[] SystemCode = await NfcF.GetSystemCode();
				//
				//	// TODO
				//}
				//else if (Interface is INfcVInterface NfcV)
				//{
				//	sbyte DsfId = await NfcV.GetDsfId();
				//	short ResponseFlags = await NfcV.GetResponseFlags();
				//
				//	// TODO
				//}
				//else if (Interface is INfcBarcodeInterface Barcode)
				//{
				//	byte[] Data = await Barcode.ReadAllData();
				//
				//	// TODO
				//}
				//else if (Interface is INdefFormatableInterface NdefFormatable)
				//{
				//	// TODO
				//}
				//else if (Interface is IMifareUltralightInterface MifareUltralight)
				//{
				//	byte[] Data = await MifareUltralight.ReadAllData();
				//
				//	// TODO
				//}
				//else if (Interface is IMifareClassicInterface MifareClassic)
				//{
				//	byte[] Data = await MifareClassic.ReadAllData();
				//
				//	// TODO
				//}
			}
		}
	}
}
