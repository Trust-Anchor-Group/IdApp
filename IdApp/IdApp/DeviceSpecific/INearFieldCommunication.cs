using System;
using System.ComponentModel;

namespace IdApp.DeviceSpecific
{
	/// <summary>
	/// Interface for Near-Fieald Communication (NFC).
	/// </summary>
	public interface INearFieldCommunication : INotifyPropertyChanged
	{
		/// <summary>
		/// If NFC Feature is available.
		/// </summary>
		bool Available { get; }
		
		/// <summary>
		/// If NFC is enabled.
		/// </summary>
		bool Enabled { get; }

		/// <summary>
		/// If Writing to NFC Tags is supported.
		/// </summary>
		bool SupportsWriting { get; }

		/// <summary>
		/// If the adapter is currently listening.
		/// </summary>
		bool Listening { get; }

		/// <summary>
		/// Starts listening for NFC Tags.
		/// </summary>
		void StartListening();

		/// <summary>
		/// Stops listening for NFC Tags.
		/// </summary>
		void StopListening();
	}
}
