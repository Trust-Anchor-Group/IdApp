using IdApp.DeviceSpecific;
using System;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.Services.Nfc
{
	/// <summary>
	/// Near-Field Communication (NFC) Service.
	/// </summary>
	[Singleton]
	public class NfcService : ServiceReferences, INfcService, IDisposable
	{
		private readonly INearFieldCommunication nfc;

		/// <summary>
		/// Near-Field Communication (NFC) Service.
		/// </summary>
		public NfcService()
			: base()
		{
			this.nfc = DependencyService.Get<INearFieldCommunication>();
			this.nfc.PropertyChanged += Nfc_PropertyChanged;
		}

		private void Nfc_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			if (this.Listening)
				this.StopListening();
		}

		/// <summary>
		/// If NFC Feature is available.
		/// </summary>
		public bool Available => this.nfc?.Available ?? false;

		/// <summary>
		/// If NFC is enabled.
		/// </summary>
		public bool Enabled => this.nfc?.Enabled ?? false;

		/// <summary>
		/// If Writing to NFC Tags is supported.
		/// </summary>
		public bool SupportsWriting => this.nfc?.SupportsWriting ?? false;

		/// <summary>
		/// If the adapter is currently listening.
		/// </summary>
		public bool Listening => this.nfc?.Listening ?? false;

		/// <summary>
		/// Starts listening for NFC Tags.
		/// </summary>
		public void StartListening() => this.nfc?.StartListening();

		/// <summary>
		/// Stops listening for NFC Tags.
		/// </summary>
		public void StopListening() => this.nfc?.StopListening();
	}
}
