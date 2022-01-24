using Android.Nfc;
using IdApp.DeviceSpecific.Nfc.Records;
using System.Text;

namespace IdApp.Android.Nfc.Records
{
	/// <summary>
	/// External Type encoded NDEF Record.
	/// </summary>
	public class ExternalTypeRecord : Record, INdefExternalTypeRecord
	{
		private readonly string type;
		private readonly byte[] data;

		/// <summary>
		/// External Type encoded NDEF Record.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		public ExternalTypeRecord(NdefRecord Record)
			: base(Record)
		{
			this.type = Encoding.UTF8.GetString(Record.GetTypeInfo());
			this.data = Record.GetPayload();
		}

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.ExternalType;

		/// <summary>
		/// External Type
		/// </summary>
		public string ExternalType => this.type;

		/// <summary>
		/// Data Payload
		/// </summary>
		public byte[] Data => this.data;
	}
}