using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NFC V Interfaces.
	/// </summary>
	public class NfcVInterface : NfcInterface, INfcVInterface
	{
		private readonly NfcV nfcV;

		/// <summary>
		/// Class handling NFC V Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NfcVInterface(Tag Tag)
			: base(Tag, NfcV.Get(Tag))
		{
			this.nfcV = (NfcV)this.technology;
		}

		/// <summary>
		/// Return the DSF ID bytes from tag discovery.
		/// </summary>
		public async Task<sbyte> GetDsfId()
		{
			await this.OpenIfClosed();
			return this.nfcV.DsfId;
		}

		/// <summary>
		/// Return the Response Flag bytes from tag discovery.
		/// </summary>
		public async Task<short> GetResponseFlags()
		{
			await this.OpenIfClosed();
			return this.nfcV.ResponseFlags;
		}
	}
}