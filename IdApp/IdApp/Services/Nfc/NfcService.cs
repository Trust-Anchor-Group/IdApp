using IdApp.DeviceSpecific.Nfc;
using IdApp.DeviceSpecific.Nfc.Extensions;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

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
			foreach (INfcInterface Interface in Tag.Interfaces)
			{
				if (Interface is IIsoDepInterface Iso14443_4)
				{
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

						byte[] C = new byte[8 + 8 + 16];

						Array.Copy(Rnd1, 0, C, 0, 8);
						Array.Copy(Challenge, 0, C, 8, 8);
						Array.Copy(Rnd2, 0, C, 16, 16);

					}
				}
			}
		}
	}
}
