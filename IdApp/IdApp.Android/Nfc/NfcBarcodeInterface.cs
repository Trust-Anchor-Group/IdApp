using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NFC Bardcode Interfaces.
	/// </summary>
	public class NfcBarcodeInterface : NfcInterface, INfcBarcodeInterface
	{
		private readonly NfcBarcode nfcBarcode;

		/// <summary>
		/// Class handling NFC Bardcode Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NfcBarcodeInterface(Tag Tag)
			: base(Tag, NfcBarcode.Get(Tag))
		{
			this.nfcBarcode = (NfcBarcode)this.technology;
		}

		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		public async Task<byte[]> ReadAllData()
		{
			await this.OpenIfClosed();

			return this.nfcBarcode.GetBarcode();
		}
	}
}