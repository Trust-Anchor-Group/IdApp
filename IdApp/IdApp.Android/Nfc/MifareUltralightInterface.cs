using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling MifareUltralight NFC Interfaces.
	/// </summary>
	public class MifareUltralightInterface : NfcInterface, IMifareUltralightInterface
	{
		private readonly MifareUltralight mifareUltralight;

		/// <summary>
		/// Class handling MifareUltralight NFC Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public MifareUltralightInterface(Tag Tag)
			: base(Tag, MifareUltralight.Get(Tag))
		{
			this.mifareUltralight = (MifareUltralight)this.technology;
		}

		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		public async Task<byte[]> ReadAllData()
		{
			await this.OpenIfClosed();

			MifareUltralightType Type = this.mifareUltralight.Type;
			int TotalBytes;

			switch (Type)
			{
				case MifareUltralightType.Ultralight:
				case MifareUltralightType.Unknown:
				default:
					TotalBytes = 64;
					break;

				case MifareUltralightType.UltralightC:
					TotalBytes = 192;
					break;
			}

			int PageSize = MifareUltralight.PageSize;
			byte[] Data = new byte[TotalBytes];
			int Offset = 0;

			while (Offset < TotalBytes)
			{
				byte[] Pages = await this.mifareUltralight.ReadPagesAsync(Offset / PageSize);
				int i = Math.Min(Pages.Length, TotalBytes - Offset);
				if (i <= 0)
					throw new Exception("Unexpected end of data.");

				Array.Copy(Pages, 0, Data, Offset, i);
				Offset += i;
			}

			return Data;
		}
	}
}