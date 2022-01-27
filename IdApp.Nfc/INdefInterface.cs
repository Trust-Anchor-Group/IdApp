using IdApp.Nfc.Records;
using System.Threading.Tasks;

namespace IdApp.Nfc
{
	/// <summary>
	/// NDEF interface, for communication with an NFC Tag.
	/// </summary>
	public interface INdefInterface : INfcInterface
	{
		/// <summary>
		/// If the TAG can be made read-only
		/// </summary>
		Task<bool> CanMakeReadOnly();

		/// <summary>
		/// If the TAG is writable
		/// </summary>
		Task<bool> IsWritable();

		/// <summary>
		/// Gets the message (with records) of the NDEF tag.
		/// </summary>
		Task<INdefRecord[]> GetMessage();
	}
}
