using IdApp.DeviceSpecific.Nfc;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Nfc
{
	/// <summary>
	/// Near-Field Communication (NFC) Service.
	/// </summary>
	[Singleton]
	public class NfcService : ServiceReferences, INfcService
	{
		/// <summary>
		/// Near-Field Communication (NFC) Service.
		/// </summary>
		public NfcService()
			: base()
		{
		}

		/// <summary>
		/// Method called when a new NFC Tag has been detected.
		/// </summary>
		/// <param name="Tag">NFC Tag</param>
		public Task TagDetected(INfcTag Tag)
		{
			return Task.CompletedTask;
		}
	}
}
