using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NFC F Interfaces.
	/// </summary>
	public class NfcFInterface : NfcInterface, INfcFInterface
	{
		private readonly NfcF nfcF;

		/// <summary>
		/// Class handling NFC F Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NfcFInterface(Tag Tag)
			: base(Tag, NfcF.Get(Tag))
		{
			this.nfcF = (NfcF)this.technology;
		}

		/// <summary>
		/// Return the Manufacturer bytes from tag discovery.
		/// </summary>
		public async Task<byte[]> GetManufacturer()
		{
			await this.OpenIfClosed();
			return this.nfcF.GetManufacturer();
		}

		/// <summary>
		/// Return the System Code bytes from tag discovery.
		/// </summary>
		public async Task<byte[]> GetSystemCode()
		{
			await this.OpenIfClosed();
			return this.nfcF.GetSystemCode();
		}
	}
}