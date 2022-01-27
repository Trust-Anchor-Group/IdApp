using Android.Nfc;
using IdApp.Nfc.Records;

namespace IdApp.Android.Nfc.Records
{
	/// <summary>
	/// Abstract base class for NDEF Records.
	/// </summary>
	public abstract class Record : INdefRecord
	{
		private readonly NdefRecord record;

		/// <summary>
		/// Abstract base class for NDEF Records.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		public Record(NdefRecord Record)
		{
			this.record = Record;
		}

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public abstract NDefRecordType Type { get; }
	}
}