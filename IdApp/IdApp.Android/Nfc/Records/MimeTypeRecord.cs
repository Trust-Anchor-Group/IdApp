using Android.Nfc;
using IdApp.DeviceSpecific.Nfc.Records;

namespace IdApp.Android.Nfc.Records
{
	/// <summary>
	/// MIME Type encoded NDEF Record.
	/// </summary>
	public class MimeTypeRecord : Record, INdefMimeTypeRecord
	{
		private readonly string contentType;
		private readonly byte[] data;

		/// <summary>
		/// MIME Type encoded NDEF Record.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		public MimeTypeRecord(NdefRecord Record)
			: base(Record)
		{
			this.contentType = Record.ToMimeType();
			this.data = Record.GetPayload();
		}

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.MimeType;

		/// <summary>
		/// Content-Type
		/// </summary>
		public string ContentType => this.contentType;

		/// <summary>
		/// Data Payload
		/// </summary>
		public byte[] Data => this.data;
	}
}