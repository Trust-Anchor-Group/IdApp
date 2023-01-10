using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.Nfc;
using System;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling MifareClassic NFC Interfaces.
	/// </summary>
	public class MifareClassicInterface : NfcInterface, IMifareClassicInterface
	{
		private readonly MifareClassic mifareClassic;

		/// <summary>
		/// Class handling MifareClassic NFC Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public MifareClassicInterface(Tag Tag)
			: base(Tag, MifareClassic.Get(Tag))
		{
			this.mifareClassic = (MifareClassic)this.technology;
		}

		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		public async Task<byte[]> ReadAllData()
		{
			await this.OpenIfClosed();

			MifareClassicType Type = this.mifareClassic.Type;
			int BlockCount = this.mifareClassic.BlockCount;
			int SectorCount = this.mifareClassic.SectorCount;
			int TotalBytes = BlockCount << 4;
			byte[] Data = new byte[TotalBytes];
			int BlockIndex = 0;

			while (BlockIndex < BlockCount)
			{
				byte[] Block = await this.mifareClassic.ReadBlockAsync(BlockIndex++);
				Array.Copy(Block, 0, Data, BlockIndex << 4, 16);
			}

			return Data;
		}
	}
}