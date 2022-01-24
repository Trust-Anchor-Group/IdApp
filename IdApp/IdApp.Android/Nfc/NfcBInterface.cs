using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NFC B Interfaces.
	/// </summary>
	public class NfcBInterface : NfcInterface, INfcBInterface
	{
		private readonly NfcB nfcB;

		/// <summary>
		/// Class handling NFC B Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NfcBInterface(Tag Tag)
			: base(Tag, NfcB.Get(Tag))
		{
			this.nfcB = (NfcB)this.technology;
		}

		/// <summary>
		/// Gets Application Data from the interface.
		/// </summary>
		public async Task<byte[]> GetApplicationData()
		{
			await this.OpenIfClosed();
			return this.nfcB.GetApplicationData();
		}

		/// <summary>
		/// Gets Protocol Information from the interface.
		/// </summary>
		public async Task<byte[]> GetProtocolInfo()
		{
			await this.OpenIfClosed();
			return this.nfcB.GetProtocolInfo();
		}
	}
}