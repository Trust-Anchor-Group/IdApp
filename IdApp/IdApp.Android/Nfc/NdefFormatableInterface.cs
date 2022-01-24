using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NDEF Formatable Interfaces.
	/// </summary>
	public class NdefFormatableInterface : NfcInterface, INdefFormatableInterface
	{
		private readonly NdefFormatable ndefFormatable;

		/// <summary>
		/// Class handling NDEF Formatable Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NdefFormatableInterface(Tag Tag)
			: base(Tag, NdefFormatable.Get(Tag))
		{
			this.ndefFormatable = (NdefFormatable)this.technology;
		}
	}
}