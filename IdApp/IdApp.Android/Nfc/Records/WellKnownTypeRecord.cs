using Android.Nfc;
using IdApp.DeviceSpecific.Nfc.Records;
using System.Text;

namespace IdApp.Android.Nfc.Records
{
	/// <summary>
	/// Well-Known Type encoded NDEF Record.
	/// </summary>
	public class WellKnownTypeRecord : Record, INdefWellKnownTypeRecord
	{
		private readonly string type;
		private readonly byte[] data;

		/// <summary>
		/// Well-Known Type encoded NDEF Record.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		public WellKnownTypeRecord(NdefRecord Record)
			: base(Record)
		{
			this.type = Encoding.UTF8.GetString(Record.GetTypeInfo());
			this.data = Record.GetPayload();
		}

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.WellKnownType;

		/// <summary>
		/// NFC Well-Known Type
		/// 
		/// Reference:
		/// https://nfc-forum.org/our-work/specification-releases/specifications/nfc-forum-assigned-numbers-register/
		/// </summary>
		public string WellKnownType => this.type;

		/// <summary>
		/// Data Payload
		/// </summary>
		public byte[] Data => this.data;
	}
}