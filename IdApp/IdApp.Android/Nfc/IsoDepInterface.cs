using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.DeviceSpecific.Nfc;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling ISO DEP Interfaces.
	/// </summary>
	public class IsoDepInterface : NfcInterface, IIsoDepInterface
	{
		private readonly IsoDep isoDep;

		/// <summary>
		/// Class handling ISO DEP Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public IsoDepInterface(Tag Tag)
			: base(Tag, IsoDep.Get(Tag))
		{
			this.isoDep = (IsoDep)this.technology;
		}

		/// <summary>
		/// Return the higher layer response bytes for NfcB tags.
		/// </summary>
		public async Task<byte[]> GetHighLayerResponse()
		{
			await this.OpenIfClosed();
			return this.isoDep.GetHiLayerResponse();
		}

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		public async Task<byte[]> GetHistoricalBytes()
		{
			await this.OpenIfClosed();
			return this.isoDep.GetHistoricalBytes();
		}
	}
}