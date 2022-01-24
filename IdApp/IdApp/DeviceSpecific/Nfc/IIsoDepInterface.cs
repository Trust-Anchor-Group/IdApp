using System.Threading.Tasks;

namespace IdApp.DeviceSpecific.Nfc
{
	/// <summary>
	/// ISO DEP interface, for communication with an NFC Tag.
	/// </summary>
	public interface IIsoDepInterface : INfcInterface
	{
		/// <summary>
		/// Return the higher layer response bytes for NfcB tags.
		/// </summary>
		Task<byte[]> GetHighLayerResponse();

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		Task<byte[]> GetHistoricalBytes();
	}
}
