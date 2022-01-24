using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NFC A Interfaces.
	/// </summary>
	public class NfcAInterface : NfcInterface, INfcAInterface
	{
		private readonly NfcA nfcA;

		/// <summary>
		/// Class handling NFC A Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NfcAInterface(Tag Tag)
			: base(Tag, NfcA.Get(Tag))
		{
			this.nfcA = (NfcA)this.technology;
		}

		/// <summary>
		/// Return the ATQA/SENS_RES bytes from tag discovery.
		/// </summary>
		public async Task<byte[]> GetAtqa()
		{
			await this.OpenIfClosed();
			return this.nfcA.GetAtqa();
		}

		/// <summary>
		/// Return the SAK/SEL_RES bytes from tag discovery.
		/// </summary>
		public async Task<short> GetSqk()
		{
			await this.OpenIfClosed();
			return this.nfcA.Sak;
		}
	}
}