using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NDEF Interfaces.
	/// </summary>
	public class NdefInterface : NfcInterface, INdefInterface
	{
		private readonly Ndef ndef;

		/// <summary>
		/// Class handling NDEF Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NdefInterface(Tag Tag)
			: base(Tag, Ndef.Get(Tag))
		{
			this.ndef = (Ndef)this.technology;
		}

		/// <summary>
		/// If the TAG can be made read-only
		/// </summary>
		public async Task<bool> CanMakeReadOnly()
		{
			await this.OpenIfClosed();
			return this.ndef.CanMakeReadOnly();
		}

		/// <summary>
		/// If the TAG is writable
		/// </summary>
		public async Task<bool> IsWritable()
		{
			await this.OpenIfClosed();
			return this.ndef.IsWritable;
		}

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		public async Task<byte[]> GetMessage()
		{
			NdefMessage Message = this.ndef.NdefMessage;
			NdefRecord[] Records = Message.GetRecords();

			// TODO: Return interpreted message.

			return Message.ToByteArray();
		}
	}
}