using Android.Nfc;
using IdApp.Nfc.Records;
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
			: this(Record, Encoding.UTF8.GetString(Record.GetTypeInfo()))
		{
		}

		/// <summary>
		/// Well-Known Type encoded NDEF Record.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		/// <param name="Type">Record type.</param>
		public WellKnownTypeRecord(NdefRecord Record, string Type)
			: base(Record)
		{
			this.type = Type;
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
