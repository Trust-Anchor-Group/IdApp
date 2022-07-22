using Android.Nfc;
using IdApp.Nfc.Records;

namespace IdApp.Android.Nfc.Records
{
	/// <summary>
	/// Absolute URI NDEF Record.
	/// </summary>
	public class UriRecord : Record, INdefUriRecord
	{
		private readonly string uri;

		/// <summary>
		/// Absolute URI NDEF Record.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		public UriRecord(NdefRecord Record)
			: base(Record)
		{
			this.uri = Record.ToUri().ToString();
		}

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.Uri;

		/// <summary>
		/// URI
		/// </summary>
		public string Uri => this.uri;
	}
}
